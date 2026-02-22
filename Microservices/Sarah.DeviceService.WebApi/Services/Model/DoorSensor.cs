using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

#pragma warning disable CS4014 // Intentional fire-and-forget async calls in property setters

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Ein Binärer Sensor, der nur 2 Zustände einnehmen kann
    /// </summary>
    public class DoorSensor : NetworkElement, IDoorSensor
    {
        private readonly NetworkElementPublisher _publisher = null!;
        private SensorData _battery = null!;
        private SensorData _Temperature = null!;
        private DoorSensorState _state;


        /// <summary>
        ///
        /// </summary>
        public override string ClassDescription => "Tür/Fenstersensor";


        /// <summary>
        /// State Info für die Visualisierung
        /// </summary>
        public override string StateInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (this.State == DoorSensorState.Geschlossen)
                {
                    sb.Append("<p>🚪✔ Geschlossen</p>");
                }
                else if (this.State == DoorSensorState.Offen)
                {
                    if (LastOpenDuration.HasValue)
                    {
                        sb.Append("<p>🚪❌ Offen seit " + Math.Round(LastOpenDuration.Value.TotalMinutes, 1) + " Minuten </p>");
                    }
                    else
                    {
                        sb.Append("<p>🚪❌ Offen</p>");
                    }
                }
                else
                {
                    sb.Append("<p>🚪❓ Unbekannt</p>");
                }

                if (!String.IsNullOrWhiteSpace(this.Battery?.ToString()))
                {
                    sb.Append("<p>Batterie: " + this.Battery.ToString() + "</p>");
                }

                if (!String.IsNullOrWhiteSpace(this.Temperature?.ToString()))
                {
                    sb.Append("<p>Temperatur: " + this.Temperature.ToString() + "</p>");
                }
                return sb.ToString();
            }
        }


        public SensorData Temperature
        {
            get { return _Temperature; }
            set
            {
                if (_Temperature != value)
                {
                    _Temperature = value;
                    _publisher.ReportEvent(this, nameof(Temperature), _Temperature?.Value.ToString(CultureInfo.CurrentCulture));
                }
            }
        }


        public DateTime? LastCloseTime { get; private set; }
        public DateTime? LastOpenTime { get; private set; }

        public TimeSpan? LastOpenDuration
        {
            get
            {
                if (LastOpenTime.HasValue && LastCloseTime.HasValue)
                {
                    if (LastOpenTime.Value > LastCloseTime.Value)
                    {
                        return DateTime.Now - LastOpenTime.Value;
                    }
                    else
                    {
                        return LastCloseTime - LastOpenTime;
                    }
                }
                else if (LastOpenTime.HasValue)
                {
                    return DateTime.Now - LastOpenTime.Value;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Der binäre offen/geschlossen Zustand
        /// </summary>
        public DoorSensorState State
        {
            get => _state;
            private set
            {
                if (this._state != value)
                {
                    this.LastStateChanged = DateTime.Now;
                    this._state = value;
                    _publisher.ReportEvent(this, nameof(State), value);

                    if(value == DoorSensorState.Offen)
                    {
                        this.LastOpenTime = DateTime.Now;
                    } else
                    {
                        this.LastCloseTime = DateTime.Now;
                    }
                }
            }
        }


        public DateTime? LastStateChanged { get; private set; }

        /// <summary>
        /// Batterieladezustand in %
        /// </summary>
        public SensorData Battery
        {
            get { return _battery; }
            set
            {
                if (_battery != value)
                {
                    _battery = value;
                    _publisher.ReportEvent(this, nameof(Battery), _battery?.Value.ToString(CultureInfo.CurrentCulture));
                }
            }
        }


        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid">ZWave Node ID</param>
        public DoorSensor(byte nodeid, NetworkElementPublisher publisher, ILogger<DoorSensor>? logger = null) : base(nodeid, logger)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// Initialisiert die Verbindung zum ZWave Gerät
        /// </summary>
        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
                Node node = deviceService.GetNode(this.NodeID) as Node;

            if (node != null)
            {
                var sensorBinary = node.GetCommandClass<SensorBinary>();
                var basic = node.GetCommandClass<Basic>();

                sensorBinary.Changed += OnSensorBinaryChanged;
                basic.Changed += OnBasicChanged;


                Battery batteryCmd = node.GetCommandClass<Battery>();
                batteryCmd.Changed += (sender, e) => this.Battery = new SensorData(e.Report.Value, "%");

                /* Aktuelle Temperaturmessung */

                SensorMultiLevel sensorCmd = node.GetCommandClass<SensorMultiLevel>();
                sensorCmd.Changed += this.SensorCmd_Changed;

                /* Für den Fibaro Door Sensor 2 muss der Controller für die zweite Association Group als Ziel für die Benachrichtigungen gesetzt werden, sonst kommt nichts an */

                //if (node.NodeID == 29)
                //{

                //    try
                //    {
                //        /* Read initial values */
                //        Association ass = node.GetCommandClass<Association>();
                //        var groups = await ass.GetGroups();
                //        await ass.Add(0, 1);
                //        await ass.Add(1, 1);
                //        await ass.Add(2, 1);
                //        await ass.Add(3, 1);
                //        await ass.Add(4, 1);
                //        await ass.Add(5, 1);
                //        groups = await ass.GetGroups();
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger?.LogDebug(ex.Message);
                //    }
                //}

                //BatteryReport batReport = await battery.Get();
                //this.BatteryLevel = batReport.Value;
            }

            return Task.CompletedTask;
        }
        private void SensorCmd_Changed(object? sender, ReportEventArgs<SensorMultiLevelReport> e)
        {
            _logger?.LogDebug("Door-Sensor Data " + e.Report.Type + " from node " + this.NodeID + " received");

            switch (e.Report.Type)
            {
                case SensorType.Temperature:
                    this.Temperature = new SensorData(e.Report.Value, e.Report.Unit);
                    break;
                default:
                    _logger?.LogWarning("WARNING: Door-Sensor Data with unknown type " + e.Report.Type + " from node " + this.NodeID + " received");
                    break;
            }

        }

        private void OnBasicChanged(object? sender, ReportEventArgs<BasicReport> e)
        {
            _logger?.LogDebug($"Basic report of Node {e.Report.Node:D3} changed to [{e.Report}]");
            if (e.Report.CurrentValue == 0)
            {
                this.State = DoorSensorState.Geschlossen;
            }
            else
            {
                this.State = DoorSensorState.Offen;
            }
        }

        private void OnSensorBinaryChanged(object? sender, ReportEventArgs<SensorBinaryReport> e)
        {
            _logger?.LogDebug($"SensorBinary report of Node {e.Report.Node:D3} changed to [{e.Report}]");
            if (e.Report.Value)
            {
                this.State = DoorSensorState.Geschlossen;
            }
            else
            {
                this.State = DoorSensorState.Offen;
            }
        }
    }
}
