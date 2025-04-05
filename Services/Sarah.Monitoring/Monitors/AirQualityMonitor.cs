using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Sarah.Logging;
using Sarah.API.Interfaces.Service;
using Sarah.Data.Models;
using Sarah.API.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Überwachungsdienst für die Luftqualität in Räumen
    /// </summary>
    internal class AirQualityMonitor(IDBService _db, IEventProcessingService _events, IDeviceService _devices) : ICanSelfTest, INetworkEventSubscriber, IMonitor
    {
        private bool IsRunning { get; set; }
        private DateTime LastUpdate { get; set; }

        private Dictionary<byte,SurveillanceTask> CurrentAirQualityTasks { get; } = new Dictionary<byte, SurveillanceTask>();

        /// <summary>
        /// Override für die SilentTime der Luftqualtität, damit der Sensor morgens nicht nervt
        /// </summary>
#if DEBUG
        internal static bool IsInSilentTime => false;

#else
        internal static bool IsInSilentTime => DateTime.Now.IsInSilentTime(_config) || DateTime.Now.Hour < 9 || DateTime.Now.Hour >= 20;

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
        public Task Start()
        {
            _events.SubscribeNetworkEventAsync(this);
            this.IsRunning = true;
            Logger.Instance.LogDebug("AirQualityMonitor gestartet und als Provider registriert.");

            return Task.CompletedTask;
        }


        /// <summary>
        /// Sensorwertänderngen per Pub/Sub empfangen
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public async Task Notify(NetworkEvent e)
        {
            try
            {
                DeviceInfo? device = await _db.Devices.FirstOrDefaultAsync(item => item.NodeID == e.SourceNodeId);

                if (device != null)
                {
                    IMultiSensor? sensor = device.GetNetworkItem(_devices) as IMultiSensor;

                    if (sensor != null
                        && IsRelevantProperty(e.Property))
                    {
                        Room? room;
                        if (device.Id_Room.HasValue)
                        {
                            room = await _db.Rooms.FindAsync(device.Id_Room);
                        }
                        else
                        {
                            room = null;
                        }


                        if (sensor.IsAirQualityLevelWarning())
                        {
                            if (!this.CurrentAirQualityTasks.ContainsKey(e.SourceNodeId))
                            {
                                this.CurrentAirQualityTasks.Add(e.SourceNodeId, new SurveillanceTask(device, room, _devices, _events));
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
                                    //NotificationEngine.Instance.Voice.Say("Die Luftqualität im " + room.Name + " ist wiederhergestellt."
                                    //    , NotificationEngine.Speaker1);
                                    await _events.PublishAirQualityEventAsync(new AirQualityChangedEvent(sensor.NodeID, 
                                        AirQualitityLevel.OK, "Die Luftqualität im " + room?.Name + " ist wiederhergestellt.", room?.Name ?? "", "AirQualityChanged"));

                                }
                            }
                        }
                        this.LastUpdate = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Fehler beim Aktualisieren der Luftqualitätszustände: " + ex.Message);
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

            private readonly IDeviceService _devices;
            private readonly IEventProcessingService _events;

            public DeviceInfo Device { get; private set; }
            public Room? Room { get; private set; }
            private CancellationTokenSource? UpdateCancellationTokenSource { get; set; }
            private Task? Task { get; set; }

            public SurveillanceTask(DeviceInfo device, Room? room, IDeviceService devices, IEventProcessingService events)
            {
                this._devices = devices;
                this._events = events;
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
                Logger.Instance.LogDebug("Beende überwachung der Luftqualität: " + this.Device.Name + "... ");
                this.UpdateCancellationTokenSource?.Cancel();
            }

            private async void Tick()
            {
                try
                {
                    Logger.Instance.LogDebug("Starte überwachung der Luftqualität: " + this.Device.Name + "... ");

                    while (!UpdateCancellationTokenSource?.Token.IsCancellationRequested == true)
                    {
                        List<string> msg = new List<string>();
                        IMultiSensor sensor = (IMultiSensor)this.Device.GetNetworkItem(_devices);

                        Tuple<AirQualitityLevel, string> co2 = new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, "");
                        Tuple<AirQualitityLevel, string> voc = new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, ""); 
                        Tuple<AirQualitityLevel, string> humidity = new Tuple<AirQualitityLevel, string>(AirQualitityLevel.OK, ""); 


                        /* Check Humidity */
                        humidity = AirQualityDefinitions.GetHumidityLevel(sensor.RelativeHumidity.Value);
                        if (humidity.Item1 > AirQualitityLevel.OK)
                        {
                            msg.Add(humidity.Item2);
                        }

                        /* Check CO² */
                        co2 = AirQualityDefinitions.GetCo2Level(sensor.CO2.Value);
                        if (co2.Item1 > AirQualitityLevel.OK)
                        {
                            msg.Add(co2.Item2);
                        }

                        /* Check VOC */
                        voc = AirQualityDefinitions.GetVocLevel(sensor.VolatileOrganicCompounds.Value);
                        if (voc.Item1 > AirQualitityLevel.OK)
                        {
                            msg.Add(voc.Item2);
                        }

                        /* SilentHours beachten */
                        if (!AirQualityMonitor.IsInSilentTime)
                        {
                            //NotificationEngine.Instance.Voice.Say("Meine Sensoren melden schlechte Luftqualität im " + this.Room.Name + ": " 
                            //    + String.Join(". " + Environment.NewLine, msg)
                            //    , NotificationEngine.Speaker1);

                            AirQualitityLevel badestLevel = new AirQualitityLevel[] { voc.Item1, co2.Item1, humidity.Item1 }
                                .OrderByDescending(item => item).First();
                            await _events.PublishAirQualityEventAsync(new AirQualityChangedEvent(sensor.NodeID, badestLevel, String.Join(". " + Environment.NewLine, msg), 
                                (this.Room?.Name ?? ""), "AirQualityChanged"));
                        }

                        /* warten uns später nochmal bescheid sagen */
                        await Task.Delay(_WarnInterval);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException(ex);
                }
            }
        }
    }
    internal static class MultiSensorExtensions
    {
        public static bool IsAirQualityLevelWarning(this IMultiSensor sensor)
        {
            if (sensor == null)
            {
                return false;
            }
            else
            {
                bool isWarning = false;

                /* Check Humidity */
                isWarning |= AirQualityDefinitions.GetHumidityLevel(sensor.RelativeHumidity.Value).Item1 > AirQualitityLevel.OK;
                

                /* Check CO² */
                isWarning |= AirQualityDefinitions.GetCo2Level(sensor.CO2.Value).Item1 > AirQualitityLevel.OK;

                /* Check VOC */
                isWarning |= AirQualityDefinitions.GetVocLevel(sensor.VolatileOrganicCompounds.Value).Item1 > AirQualitityLevel.OK;
                

                return isWarning;
            }
        }
    }
}
