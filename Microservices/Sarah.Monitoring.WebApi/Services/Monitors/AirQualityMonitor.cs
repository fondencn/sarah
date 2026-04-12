using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces.Service;
using Sarah.API.Extensions;
using Microsoft.Extensions.Configuration;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.ServiceClients;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Überwachungsdienst für die Luftqualität in Räumen
    /// </summary>
    internal class AirQualityMonitor : ICanSelfTest, IMonitor
    {
        private readonly IReadOnlyList<RoomDto> _roomSnapshot;
        private readonly RabbitMQClient _rabbitMQ;
        private readonly ILogger<AirQualityMonitor> _logger;
        private readonly IConfiguration _config;
        private static IConfiguration? _staticConfig;
        private readonly DeviceServiceClient _deviceServiceClient;

        public AirQualityMonitor(IReadOnlyList<RoomDto> roomSnapshot, RabbitMQClient rabbitMQ, IConfiguration config, ILogger<AirQualityMonitor> logger, DeviceServiceClient deviceServiceClient)
        {
            _roomSnapshot = roomSnapshot;
            _rabbitMQ = rabbitMQ;
            _config = config;
            _staticConfig = config;
            _logger = logger;
            _deviceServiceClient = deviceServiceClient;
        }

        private bool IsRunning { get; set; }
        private DateTime LastUpdate { get; set; }

        private Dictionary<byte,SurveillanceTask> CurrentAirQualityTasks { get; } = new Dictionary<byte, SurveillanceTask>();

        /// <summary>
        /// Override für die SilentTime der Luftqualtität, damit der Sensor morgens nicht nervt
        /// </summary>
#if DEBUG
        internal static bool IsInSilentTime => false;

#else
        internal static bool IsInSilentTime => (_staticConfig != null && DateTime.Now.IsInSilentTime(_staticConfig)) || DateTime.Now.Hour < 9 || DateTime.Now.Hour >= 20;

#endif

        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~AirQualityMonitor()
        {
            if (this.CurrentAirQualityTasks.Any())
            {
                this.CurrentAirQualityTasks.Values.ToList().ForEach(task => task.Cancel());
                this.CurrentAirQualityTasks.Clear();
            }
        }

        /// <summary>
        /// Startet die Überwachung in einem eigenen Task
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            if (this.IsRunning)
            {
                return;
            }

            // 1) Air quality still listens to generic network.events messages because CO2/VOC/humidity are published as generic property events.
            await _rabbitMQ.SubscribeAsync<NetworkEventMessage<object>>(
                topic: MessageTopics.NetworkEvents,
                onMessage: async message =>
                {
                    var networkEvent = new NetworkEvent<object>(message.SourceNodeId, message.Property, null);
                    await HandleNetworkEvent(networkEvent);
                },
                exchange: MessageTopics.NetworkEvents);

            this.IsRunning = true;
            _logger.LogDebug("AirQualityMonitor gestartet und als Provider registriert.");
        }


        /// <summary>
        /// Sensorwertänderngen per Pub/Sub empfangen
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task HandleNetworkEvent(NetworkEvent e)
        {
            try
            {
                if (!IsRelevantProperty(e.Property))
                    return;

                DeviceDto? liveDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync(e.SourceNodeId);
                AirQualityStateDto? airQuality = liveDevice?.AirQuality;

                if (liveDevice != null && airQuality != null)
                {
                    RoomDto? room = liveDevice.RoomId.HasValue
                        ? _roomSnapshot.FirstOrDefault(r => r.Id == liveDevice.RoomId.Value)
                        : null;

                    if (airQuality.IsAirQualityLevelWarning())
                    {
                        if (!this.CurrentAirQualityTasks.ContainsKey(e.SourceNodeId))
                        {
                            this.CurrentAirQualityTasks.Add(e.SourceNodeId, new SurveillanceTask(liveDevice, room, _deviceServiceClient, _rabbitMQ, _logger));
                        }
                    }
                    else
                    {
                        if (this.CurrentAirQualityTasks.ContainsKey(e.SourceNodeId))
                        {
                            this.CurrentAirQualityTasks[e.SourceNodeId].Cancel();
                            this.CurrentAirQualityTasks.Remove(e.SourceNodeId);

                            /* SilentHours beachten */
                            if (!IsInSilentTime)
                            {
                                await _rabbitMQ.PublishAsync(new AirQualityChangedMessage(e.SourceNodeId,
                                    (AirQualityLevel)AirQualitityLevel.OK, "Die Luftqualität im " + room?.Name + " ist wiederhergestellt.", room?.Name ?? ""));
                            }
                        }
                    }
                    this.LastUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren der Luftqualitätszustände");
            }
        }



        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (!this.IsRunning)
            {
                yield return new SelfTestResult(true, "Luftqualität", "Nicht gestartet");
            }
            if ((DateTime.Now - this.LastUpdate) > TimeSpan.FromDays(1))
            {
                yield return new SelfTestResult(true, "Luftqualität", "Letzter Sensorwert vor mehr als 1 Tag verarbeitet.");
            }
            if (this.CurrentAirQualityTasks.Any())
            {
                yield return new SelfTestResult(true, "Luftqualität", "Momentan melden " + this.CurrentAirQualityTasks.Count + " Sensoren Warnungen");
            }
        }

        /// <summary>
        /// Gibt an, ob die geänderte Property für diese Überwachung relevant ist.
        /// </summary>
        /// <param name="propertyName">name der verändeten Property</param>
        /// <returns>true, wenn die Propery für diesen Überwachungsdienst interessant ist, sonst false</returns>
        private static bool IsRelevantProperty(string propertyName)
        {
            return String.Equals(propertyName, nameof(IMultiSensor.CO2))
                || String.Equals(propertyName, nameof(IMultiSensor.VolatileOrganicCompounds))
                || String.Equals(propertyName, nameof(IMultiSensor.RelativeHumidity));
        }









        private class SurveillanceTask
        {
            private static readonly TimeSpan _WarnInterval = TimeSpan.FromMinutes(30);

            private readonly DeviceServiceClient _deviceServiceClient;
            private readonly RabbitMQClient _rabbitMQ;
            private readonly ILogger<AirQualityMonitor> _logger;

            public DeviceDto Device { get; private set; }
            public RoomDto? Room { get; private set; }
            private CancellationTokenSource? UpdateCancellationTokenSource { get; set; }
            private Task? Task { get; set; }

            public SurveillanceTask(DeviceDto device, RoomDto? room, DeviceServiceClient deviceServiceClient, RabbitMQClient rabbitMQ, ILogger<AirQualityMonitor> logger)
            {
                this._deviceServiceClient = deviceServiceClient;
                this._rabbitMQ = rabbitMQ;
                this._logger = logger;
                this.Device = device;
                this.Room = room;

                CancellationTokenSource cts = new CancellationTokenSource();
                this.UpdateCancellationTokenSource = cts;

                this.Task = Task.Run(Tick, cts.Token);
            }

            ~SurveillanceTask()
            {
                this.UpdateCancellationTokenSource?.Cancel();
                this.UpdateCancellationTokenSource?.Dispose();
                this.UpdateCancellationTokenSource = null;
            }

            public void Cancel()
            {
                _logger.LogDebug("Beende überwachung der Luftqualität: {DeviceName}...", this.Device.Name);
                this.UpdateCancellationTokenSource?.Cancel();
            }

            private async void Tick()
            {
                try
                {
                    _logger.LogDebug("Starte überwachung der Luftqualität: {DeviceName}...", this.Device.Name);

                    while (!UpdateCancellationTokenSource?.Token.IsCancellationRequested == true)
                    {
                        DeviceDto? liveDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync((byte)this.Device.NodeId);
                        AirQualityStateDto? airQuality = liveDevice?.AirQuality;

                        if (airQuality == null)
                        {
                            _logger.LogWarning("Überwachung der Luftqualität für {DeviceName} konnte nicht fortgesetzt werden, da der Sensor nicht mehr erreichbar ist.", this.Device.Name);
                            return;
                        }

                        List<string> msg = new List<string>();
                        Tuple<AirQualitityLevel, string> co2 = new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "");
                        Tuple<AirQualitityLevel, string> voc = new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "");
                        Tuple<AirQualitityLevel, string> humidity = new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "");

                        /* Check Humidity */
                        if (airQuality.RelativeHumidity.HasValue)
                        {
                            humidity = AirQualityDefinitions.GetHumidityLevel(airQuality.RelativeHumidity.Value);
                            if (humidity.Item1 > AirQualitityLevel.OK)
                                msg.Add(humidity.Item2);
                        }

                        /* Check CO² */
                        if (airQuality.CO2.HasValue)
                        {
                            co2 = AirQualityDefinitions.GetCo2Level(airQuality.CO2.Value);
                            if (co2.Item1 > AirQualitityLevel.OK)
                                msg.Add(co2.Item2);
                        }

                        /* Check VOC */
                        if (airQuality.VolatileOrganicCompounds.HasValue)
                        {
                            voc = AirQualityDefinitions.GetVocLevel(airQuality.VolatileOrganicCompounds.Value);
                            if (voc.Item1 > AirQualitityLevel.OK)
                                msg.Add(voc.Item2);
                        }

                        /* SilentHours beachten */
                        if (!AirQualityMonitor.IsInSilentTime)
                        {
                            AirQualitityLevel badestLevel = new AirQualitityLevel[] { voc.Item1, co2.Item1, humidity.Item1 }
                                .OrderByDescending(item => item).First();
                            await _rabbitMQ.PublishAsync(new AirQualityChangedMessage((byte)this.Device.NodeId, (AirQualityLevel)badestLevel,
                                String.Join(". " + Environment.NewLine, msg),
                                (this.Room?.Name ?? "")));
                        }

                        /* warten uns später nochmal bescheid sagen */
                        await Task.Delay(_WarnInterval);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred");
                }
            }
        }
    }
    internal static class MultiSensorExtensions
    {
        public static bool IsAirQualityLevelWarning(this AirQualityStateDto? airQuality)
        {
            if (airQuality == null)
                return false;

            bool isWarning = false;

            if (airQuality.RelativeHumidity.HasValue)
                isWarning |= AirQualityDefinitions.GetHumidityLevel(airQuality.RelativeHumidity.Value).Item1 > AirQualitityLevel.OK;

            if (airQuality.CO2.HasValue)
                isWarning |= AirQualityDefinitions.GetCo2Level(airQuality.CO2.Value).Item1 > AirQualitityLevel.OK;

            if (airQuality.VolatileOrganicCompounds.HasValue)
                isWarning |= AirQualityDefinitions.GetVocLevel(airQuality.VolatileOrganicCompounds.Value).Item1 > AirQualitityLevel.OK;

            return isWarning;
        }
    }
}
