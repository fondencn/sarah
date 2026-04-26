using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.API.Extensions;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.ServiceClients;

namespace Sarah.Monitoring.Monitors
{
    internal class BatteryMonitor(IReadOnlyList<RoomDto> _roomSnapshot, DeviceServiceClient _deviceServiceClient, RabbitMQClient _rabbitMQ, IConfiguration _config, ILogger<BatteryMonitor> _logger) : ICanSelfTest, IMonitor
    {
        private static readonly TimeSpan _UpdateInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _WarnInterval = TimeSpan.FromHours(4);
        private Task? UpdateTask { get; set; }
        private CancellationTokenSource? UpdateCancellationTokenSource { get; set; }
        private DateTime _lastUpdate;
        private DateTime _lastWarning;

        private List<BatteryInfo> CurrentBatteryInfos { get; } = new List<BatteryInfo>();

       
        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~BatteryMonitor()
        {
            if (this.UpdateTask != null && this.UpdateTask.Status == TaskStatus.Running)
            {
                this.UpdateCancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        /// Startet die Überwachung in einem eigenen Task
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            if (this.UpdateTask != null)
            {
                throw new InvalidOperationException("BatteryMonitor wurde bereits gestartet und kann nicht noch einmal gestartet werden.");
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            this.UpdateCancellationTokenSource = cts;
            this.UpdateTask = Task.Run(Update, cts.Token);

            _logger.LogDebug("BatteryMonitor gestartet und als Provider registriert.");

            return Task.CompletedTask;
        }
        /// <summary>
        /// Regelmäßig Updateroutine dieser Klasse
        /// </summary>
        private async void Update()
        {
            bool isFirstRun = true;
            while (!this.UpdateCancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                if (isFirstRun)
                {
                    isFirstRun = false;
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
                else
                {
                    await Task.Delay(_UpdateInterval);
                }
                if (this.UpdateCancellationTokenSource?.Token.IsCancellationRequested == true) break;

                await UpdateCurrentBatteryStatsAsync();
                await RaiseWarningsIfNecessary();
            }
        }

        public string GetCurrentBatteryInfoString()
        {
            StringBuilder sbWarnings = new StringBuilder();
            foreach (BatteryInfo info in this.CurrentBatteryInfos)
            {
                if (info.BatteryLevel <= 10)
                {
                    sbWarnings.AppendLine($"{info.NodeDescription}: Weniger als {info.BatteryLevel } % Batterieladung");
                }
            }

            if (sbWarnings.Length > 0)
            {
                sbWarnings.Insert(0, "Achtung, der Ladezustand einiger Geräte ist kritisch: " + Environment.NewLine);
            } else
            {
                sbWarnings.Append("Der Ladezustand aller Geräte ist in Ordnung.");
            }

            return sbWarnings.ToString();
        }

        private async Task RaiseWarningsIfNecessary()
        {
            try
            {
                DateTime now = DateTime.Now;
                bool warnNow = (now - this._lastWarning) > _WarnInterval;

                /* SilentHours beachten */
                warnNow &= !now.IsInSilentTime(_config);

                if (warnNow)
                {
                    StringBuilder sbWarnings = new StringBuilder();
                    foreach(BatteryInfo info in this.CurrentBatteryInfos)
                    {
                        if (info.BatteryLevel < 5 )
                        {
                            sbWarnings.AppendLine($"{info.NodeDescription}: Batterie leer");
                        }
                        else if (info.BatteryLevel <= 10)
                        {
                            sbWarnings.AppendLine($"{info.NodeDescription}: Weniger als {info.BatteryLevel } % Batterieladung");
                        }
                    }

                    if(sbWarnings.Length > 0)
                    {
                        sbWarnings.Insert(0, "Achtung, Ladezustand kritisch: " + Environment.NewLine);
                        _logger.LogWarning(sbWarnings.ToString());
                        var batteryWarning = new BatteryWarningMessage
                        {
                            Warnings = this.CurrentBatteryInfos
                                .Where(i => i.BatteryLevel <= 10)
                                .Select(i => new BatteryDeviceWarning
                                {
                                    DeviceName = i.NodeDescription,
                                    BatteryLevel = i.BatteryLevel
                                })
                                .ToList()
                        };
                        await _rabbitMQ.PublishAsync(batteryWarning);
                    }

                    _lastWarning = DateTime.Now;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Battery Warnings");
            }
        }

        private async Task UpdateCurrentBatteryStatsAsync()
        {
            try
            {
                this.CurrentBatteryInfos.Clear();
                // 3) Battery monitoring currently stays polling-based and reads live DTO data via DeviceServiceClient.
                var batteryDrivenDevices = (await _deviceServiceClient.GetAllDevicesAsync())
                    .Where(d => d.Battery?.Level is not null)
                    .ToList();

                if (batteryDrivenDevices.Any())
                {
                    foreach (DeviceDto device in batteryDrivenDevices)
                    {
                        float batteryPercentage = device.Battery!.Level!.Value;
                        RoomDto? room = device.RoomId.HasValue
                            ? _roomSnapshot.FirstOrDefault(r => r.Id == device.RoomId!.Value)
                            : null;
                        this.CurrentBatteryInfos.Add(new BatteryInfo((byte)device.NodeId, device.Name + (room != null ? " im " + room.Name : String.Empty), batteryPercentage));
                    }
                }
                this._lastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim aktualisieren der Batterieinfos");
                _logger.LogWarning("StackTrace: {StackTrace}", ex.StackTrace);
            }
        }

        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (this.CurrentBatteryInfos == null)
            {
                yield return new SelfTestResult(true, "Sensorbatterie-Überwachung", "Keine Informationen über den Ladezustand der Batteriesensoren geladen");
            }
            if ((DateTime.Now - this._lastUpdate) > TimeSpan.FromDays(2))
            {
                yield return new SelfTestResult(true, "Sensorbatterie-Überwachung", "Die Daten sind älter als 2 Tage");
            }
            foreach(var lowBatItem in this.CurrentBatteryInfos!.Where(item => item.BatteryLevel <= 10))
            {
                yield return new SelfTestResult(true, "Sensorbatterie-Überwachung", lowBatItem.NodeDescription + " Batterie bei " + lowBatItem.BatteryLevel + "%");
            }
        }
    }

    public class BatteryInfo
    {
        public byte NodeId { get; }
        public string NodeDescription { get; }
        public float BatteryLevel { get; }

        public BatteryInfo(byte nodeid, string text, float level)
        {
            this.NodeId = nodeid;
            this.NodeDescription = String.IsNullOrWhiteSpace(text) ? "Gerät " + nodeid : text;
            this.BatteryLevel = level;
        }
    }
}
