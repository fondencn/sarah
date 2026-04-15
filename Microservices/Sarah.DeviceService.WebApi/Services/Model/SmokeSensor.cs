using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System.Text;
using ZWave.CommandClasses;
using ZWave;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.Model.Extensions;
using Sarah.DeviceService.WebApi.Extensions;

namespace Sarah.DeviceService.Model
{
    public partial class SmokeSensor : NetworkElement, ISmokeSensor, IBatterySensor, ITemperatureSensor
    {
        private readonly NetworkElementPublisher _publisher;
        private DateTime LastUpdate { get; set; } = DateTime.MinValue;
        private SensorData? _temperature;
        private SensorData? _battery;
        private SensorData? _alarm;
        private SensorData? _smokeDetected;
        private SensorData? _overHeatDetected;


        public SmokeSensor(byte nodeid, NetworkElementPublisher publisher, ILogger<SmokeSensor> logger) : base(nodeid, logger)
        {
            _publisher = publisher;
        }

        public SensorData Temperature
        {
            get => _temperature ?? SensorData.Empty;
            private set { if (_temperature != value) { _temperature = value; _ = _publisher.ReportEvent(this, nameof(Temperature), value?.ToString()); LastUpdate = DateTime.Now; } }
        }

        public SensorData? Battery
        {
            get => _battery ?? SensorData.Empty;
            private set { if (_battery != value) { _battery = value; _ = _publisher.ReportEvent(this, nameof(Battery), value?.ToString()); LastUpdate = DateTime.Now; } }
        }

        public SensorData IsSmokeDetected
        {
            get => _smokeDetected ?? SensorData.Empty;
            private set { if (_smokeDetected != value) { _smokeDetected = value; _ = _publisher.ReportEvent(this, nameof(IsSmokeDetected), value?.ToString()); LastUpdate = DateTime.Now; } }
        }

        public SensorData IsOverheatingDetected
        {
            get => _overHeatDetected ?? SensorData.Empty;
            private set { if (_overHeatDetected != value) { _overHeatDetected = value; _ = _publisher.ReportEvent(this, nameof(IsOverheatingDetected), value?.ToString()); LastUpdate = DateTime.Now; } }
        }
        public SensorData Alarm { 
            get => _alarm ?? SensorData.Empty; 
            private set { if (_alarm != value) { _alarm = value; _ = _publisher.ReportEvent(this, nameof(Alarm), value?.ToString()); _ = _publisher.ReportSmokeAlarm(this, (value?.Value ?? 0) > 0); LastUpdate = DateTime.Now; } } 
        }




        /// <summary>
        /// Initialisiert das Gerät / startet die Kommunikation mit diesem Gerät
        /// </summary>
        /// <returns></returns>
        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config)
        {
            Node? node = deviceService.GetZWaveNode(this.NodeID);

            /* Register for Device events */
            try
            {
                if(node == null) 
                {
                    throw new InvalidOperationException($"Node {this.NodeID} not found in ZWave network");
                }

                _logger?.LogDebug("Initializing new Smokesensor for node " + this.NodeID + " ...");

                var basicCmd = node.GetCommandClass<Basic>();
                basicCmd.Changed += BasicCmd_Changed;

                var sensorCmd = node.GetCommandClass<SensorMultiLevel>();
                sensorCmd.Changed += this.SensorCmd_Changed;


                var alarmCmd = node.GetCommandClass<Alarm>();
                alarmCmd.Changed += this.AlarmCmd_Changed;

                var battCmd = node.GetCommandClass<Battery>();
                battCmd.Changed += this.BattCmd_Changed;

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SmokeSensor::InitializeAsync: Fehler beim registrieren der Events");
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// State Info für die Visualisierung
        /// </summary>
        public override string StateInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!String.IsNullOrWhiteSpace(this.Temperature?.ToString()))
                {
                    sb.Append("<p>Temperatur: " + this.Temperature?.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Alarm?.ToString()))
                {
                    sb.Append("<p>Alarm: " + this.Alarm.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.IsSmokeDetected?.ToString()))
                {
                    sb.Append("<p>Rauch: " + this.IsSmokeDetected.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.IsOverheatingDetected?.ToString()))
                {
                    sb.Append("<p>Hitze: " + this.IsOverheatingDetected.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Battery?.ToString()))
                {
                    sb.Append("<p>Batterie: " + this.Battery.ToString() + "</p>");
                }

                sb.Append("<p>Letzte Meldung: " + (this.LastUpdate == DateTime.MinValue ? "Nie" : this.LastUpdate.ToString()) + "</p>");
                return sb.ToString();
            }
        }

        private void BattCmd_Changed(object? sender, ReportEventArgs<BatteryReport> e)
        {
            _logger?.LogDebug("Battery value " + e.Report.Value + " from node " + this.NodeID + " received");
            this.Battery = new SensorData(e.Report.Value, "%");
        }

        private void BasicCmd_Changed(object? sender, ReportEventArgs<BasicReport> e)
        {
            _logger?.LogDebug("Basic value " + e.Report.CurrentValue + " from node " + this.NodeID + " received");
            /* Momentan nichts weiter implementiert */
        }

        private void AlarmCmd_Changed(object? sender, ReportEventArgs<AlarmReport> e)
        {
            _logger?.LogDebug("Sensor Alarm " + e.Report.Type + " from node " + this.NodeID + " received");
            this.Alarm = new SensorData(e.Report.Level, (e.Report.Level > 0 ? "🚨" : "") + (e.Report.Level > 0 ? "(" + e.Report.Type.ToString() + " Alarm)" : ""));


            if (e.Report.Type == NotificationType.Smoke || e.Report.Type == NotificationType.CarbonDioxide || e.Report.Type == NotificationType.CarbonMonoxide)
            {
                this.IsSmokeDetected = new SensorData(e.Report.Level, (e.Report.Level > 0 ? "🔥" : "") + (e.Report.Level > 0 ? "(" + e.Report.Type.ToString() + " Alarm)" : ""));

            }
            else if (e.Report.Type == NotificationType.Heat )
            {
                this.IsOverheatingDetected = new SensorData(e.Report.Level, (e.Report.Level > 0 ? "🥵" : "") + (e.Report.Level > 0 ? "(" + e.Report.Type.ToString() + " Alarm)" : ""));
            } 

        }

        private void SensorCmd_Changed(object? sender, ReportEventArgs<SensorMultiLevelReport> e)
        {
            _logger?.LogDebug("Sensor Data " + e.Report.Type + " from node " + this.NodeID + " received");
            if (e.Report.Type == SensorType.Temperature)
            {
                this.Temperature = new SensorData(e.Report.Value, e.Report.Unit);
            }
            else
            {
                _logger?.LogError("UNKNOWN Sensor Data " + e.Report.Type + " from node " + this.NodeID + " received");
            }
        }
    }
}
