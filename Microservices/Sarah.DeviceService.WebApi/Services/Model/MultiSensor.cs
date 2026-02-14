using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;
using ZWave.Devices.Aeon;

#pragma warning disable CS4014 // Intentional fire-and-forget async calls in property setters

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Ein Multisensor (z.B. Fibaro Auge)
    /// </summary>
    public class MultiSensor : NetworkElement, ITemperatureSensor, IBatterySensor, IMultiSensor
    {
        private readonly NetworkElementPublisher _publisher = null!;
        private SensorData _temperature = null!;
        private SensorData _luminance = null!;
        private SensorData _alarm = null!;
        private SensorData _unknown = null!;
        private SensorData _presence = null!;
        private SensorData _battery = null!;
        private DateTime? _firstPresenceTick;
        private SensorData _relativeHumidity = null!;
        private SensorData _dewPoint = null!;
        private SensorData _cO2 = null!;
        private SensorData _moisture = null!;
        private SensorData _volatileOrganicCompounds = null!;

        private Task UpdateNoMotionTask { get; set; } = null!;
        private Task UpdateDewpointTask { get; set; } = null!;
        private CancellationTokenSource CancellationTokenSource { get; set; } = null!;

        private IDeviceService _deviceService = null!;

        /// <summary>
        /// Temperatur
        /// </summary>
        public SensorData Temperature { get => _temperature; private set { if (_temperature != value) { _temperature = value; _publisher.ReportEvent(this, nameof(Temperature), value?.ToString()); } } }
        /// <summary>
        /// Helligkeit
        /// </summary>
        public SensorData Luminance { get => _luminance; private set { if (_luminance != value) { _luminance = value; _publisher.ReportEvent(this, nameof(Luminance), value?.ToString()); } } }
        /// <summary>
        /// Bewegungsalarm / Tamper
        /// </summary>
        public SensorData Alarm { get => _alarm; private set { if (_alarm != value) { _alarm = value; _publisher.ReportEvent(this, nameof(Alarm), value?.ToString()); } } }
        /// <summary>
        /// Catch-All für unbekannte Sensordaten
        /// </summary>
        public SensorData Unknown { get => _unknown; private set { if (_unknown != value) { _unknown = value; _publisher.ReportEvent(this, nameof(Unknown), value?.ToString()); } } }
        /// <summary>
        /// Bewegung
        /// </summary>
        public SensorData Presence
        {
            get => _presence;
            private set
            {
                if (_presence?.Value != value?.Value)
                {
                    _presence = value;
                    _publisher.ReportEvent(this, nameof(Presence), value?.ToString());

                    if (_presence.Value > 0)
                    {
                        this._firstPresenceTick = DateTime.Now;
                    }
                    else
                    {
                        this._firstPresenceTick = null;
                    }
                }
            }
        }

        /// <summary>
        /// Ladezustand der Batterie des Gerätes
        /// </summary>
        public SensorData Battery { get => _battery; private set { if (_battery != value) { _battery = value; _publisher.ReportEvent(this, nameof(Battery), value?.ToString()); } } }

        /// <summary>
        /// Luftfeutchtigkeit
        /// </summary>
        public SensorData RelativeHumidity { get => _relativeHumidity; private set { if (_relativeHumidity != value) { _relativeHumidity = value; _publisher.ReportEvent(this, nameof(RelativeHumidity), value?.ToString()); } } }

        /// <summary>
        /// Taupunkt / Kondensatpunkt
        /// </summary>
        public SensorData DewPoint { get => _dewPoint; private set { if (_dewPoint != value) { _dewPoint = value; _publisher.ReportEvent(this, nameof(DewPoint), value?.ToString()); } } }

        /// <summary>
        /// C02
        /// </summary>
        public SensorData CO2 { get => _cO2; private set { if (_cO2 != value) { _cO2 = value; _publisher.ReportEvent(this, nameof(CO2), value?.ToString()); } } }

        /// <summary>
        /// Feuchtigkeit
        /// </summary>
        public SensorData Moisture { get => _moisture; private set { if (_moisture != value) { _moisture = value; _publisher.ReportEvent(this, nameof(Moisture), value?.ToString()); } } }

        /// <summary>
        /// Flüchtige Organische Stoffe
        /// </summary>
        public SensorData VolatileOrganicCompounds { get => _volatileOrganicCompounds; private set { if (_volatileOrganicCompounds != value) { _volatileOrganicCompounds = value; _publisher.ReportEvent(this, nameof(VolatileOrganicCompounds), value?.ToString()); } } }


        private readonly Dictionary<AirQualitityLevel, string> _ColorsByWarnLevel = new System.Collections.Generic.Dictionary<AirQualitityLevel, string>()
        {
            {AirQualitityLevel.OK,  "DarkGreen"},
            {AirQualitityLevel.Warning,  "Green"},
            {AirQualitityLevel.Bad,  "Gold"},
            {AirQualitityLevel.SuperBad,  "Red"},
        };

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
                if (!String.IsNullOrWhiteSpace(this.Luminance?.ToString()))
                {
                    sb.Append("<p>Helligkeit: " + this.Luminance.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Alarm?.ToString()))
                {
                    sb.Append("<p>Alarm: " + this.Alarm.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Unknown?.ToString()))
                {
                    sb.Append("<p>Unbekannt: " + this.Unknown.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Presence?.ToString()))
                {
                    string presenceState = (this.Presence.Value > 0 ? "ja" : "nein");
                    if (_firstPresenceTick.HasValue)
                    {
                        presenceState += "(seit " + Math.Round((DateTime.Now - _firstPresenceTick.Value).TotalMinutes, 1) + "m)";
                    }
                    sb.Append("<p>Präsenz: " + presenceState + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Battery?.ToString()))
                {
                    sb.Append("<p>Batterie: " + this.Battery.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.RelativeHumidity?.ToString()))
                {
                    sb.Append("<p>Luftfeuchtigkeit: " + this.RelativeHumidity.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.DewPoint?.ToString()))
                {
                    sb.Append("<p>Taupunkt: " + this.DewPoint.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.CO2?.ToString()))
                {
                    AirQualitityLevel level = AirQualityDefinitions.GetCo2Level(this.CO2.Value).Item1;
                    string color = _ColorsByWarnLevel[level];

                    sb.Append("<p>CO²: <span style='color:" + color + "'>" + this.CO2.ToString() + "</span></p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Moisture?.ToString()))
                {
                    sb.Append("<p>Feuchtigkeit²: " + this.Moisture.ToString() + "</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.VolatileOrganicCompounds?.ToString()))
                {
                    AirQualitityLevel level = AirQualityDefinitions.GetCo2Level(this.VolatileOrganicCompounds .Value).Item1;
                    string color = _ColorsByWarnLevel[level];
                    sb.Append("<p>Org. Verbindungen: <span style='color:" + color + "'>" + this.VolatileOrganicCompounds.ToString() + "</span></p>");
                }
                return sb.ToString();
            }
        }


        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid"></param>
        public MultiSensor(byte nodeid, NetworkElementPublisher publisher, ILogger<MultiSensor>? logger = null) : base(nodeid, logger)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~MultiSensor()
        {
            this.CancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Initialisiert das Gerät / startet die Kommunikation mit diesem Gerät
        /// </summary>
        /// <returns></returns>
        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            this._deviceService = deviceService;
            Node node = deviceService.GetNode(this.NodeID) as Node;

            /* Register for Device events */
            try
            {
                if(node == null) 
                {
                    throw new InvalidOperationException($"Node {this.NodeID} not found in ZWave network");
                }
                _logger?.LogDebug("Initializing new Multisensor for node " + this.NodeID + " ...");

                var basicCmd = node.GetCommandClass<Basic>();
                basicCmd.Changed += BasicCmd_Changed;

                var sensorCmd = node.GetCommandClass<SensorMultiLevel>();
                sensorCmd.Changed += this.SensorCmd_Changed;


                var alarmCmd = node.GetCommandClass<SensorAlarm>();
                alarmCmd.Changed += this.AlarmCmd_Changed;

                var battCmd = node.GetCommandClass<Battery>();
                battCmd.Changed += this.BattCmd_Changed;



                //this.UpdateNoMotionTask = Task.Run(async () =>
                //{
                //    while (!this.UpdateNoMotionCancellationTokenSource.Token.IsCancellationRequested)
                //    {
                //        await Task.Delay(5000);
                //        this.UpdatePresence();
                //    }
                //}, cts.Token);

                CancellationTokenSource cts = new CancellationTokenSource();
                this.CancellationTokenSource = cts;
                if (this.NodeID == 31) //Eutronics Luftgütesensor
                {
                    this.UpdateNoMotionTask = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        _ = sensorCmd.Get(SensorType.DewPoint);
                        while (!this.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(10));
                            _ = sensorCmd.Get(SensorType.DewPoint);
                        }
                    }, cts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MultiSensor::InitializeAsync: Fehler beim registrieren der Events");
            }



            //try
            //{
            //    /* Check for Supported Sensors info */
            //    var sensorCmd = node.GetCommandClass<SensorMultiLevel>();
            //    if (await sensorCmd.IsSupportGetSupportedSensors())
            //    {
            //        var supportedSensors = await sensorCmd.GetSupportedSensors();
            //        this.SupportedSensors = supportedSensors.SupportedSensorTypes;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger?.LogDebug("Could not determine supported sensors for node " + this.NodeID + ": " + ex.Message);
            //}


            /* Für den Fibaro Motion Sensor 3.2 muss der Controller für alle 5 Association Groups als Ziel für die Benachrichtigungen gesetzt werden, sonst kommt nichts an */
            //try
            //{
            //    /* Read initial values */
            //    Association ass = node.GetCommandClass<Association>();
            //    var groups = await ass.GetGroups();
            //    await ass.Add(0, 1);
            //    await ass.Add(1, 1);
            //    await ass.Add(2, 1);
            //    await ass.Add(3, 1);
            //    await ass.Add(4, 1);
            //    await ass.Add(5, 1);
            //    groups = await ass.GetGroups();
            //}
            //catch (Exception ex)
            //{
            //    _logger?.LogDebug(ex.Message);
            //}


            //try
            //{
            //    /* Read initial values */
            //    var basicCmd = node.GetCommandClass<Basic>();
            //    var basicReport = await basicCmd.Get();



            //    var manuspecificCmd = node.GetCommandClass<ManufacturerSpecific>();
            //    var menuspecReport = await manuspecificCmd.Get();


            //    var sensorCmd = node.GetCommandClass<SensorMultiLevel>();

            //    var luminanceReport = await sensorCmd.Get(SensorType.Luminance);
            //    this.Luminance = new SensorData(luminanceReport.Value, luminanceReport.Unit);

            //    var temperatorReport = await sensorCmd.Get(SensorType.Temperature);
            //    this.Temperature = new SensorData(temperatorReport.Value, temperatorReport.Unit);


            //}
            //catch (Exception ex)
            //{
            //    _logger?.LogDebug(ex.Message);
            //}

            return Task.CompletedTask;
        }

        //private void UpdatePresence()
        //{
        //    if (_lastPresenceTick.HasValue)
        //    {
        //        if ((DateTime.Now - _lastPresenceTick.Value) < TimeSpan.FromMinutes(NoMotion_Minutes_Since_Last_Seen_Motion))
        //        {
        //            this.Presence = new SensorData(1, "Präsenz");
        //        }
        //        else
        //        {
        //            this.Presence = new SensorData(0, "Präsenz");
        //        }
        //    }
        //    else
        //    {
        //        this.Presence = new SensorData(0, "Präsenz");
        //    }
        //}

        private void BattCmd_Changed(object? sender, ReportEventArgs<BatteryReport> e)
        {
            _logger?.LogDebug("Battery value " + e.Report.Value + " from node " + this.NodeID + " received");

            this.Battery = new SensorData(e.Report.Value, "%");
        }

        private void BasicCmd_Changed(object? sender, ReportEventArgs<BasicReport> e)
        {
            _logger?.LogDebug("Basic value " + e.Report.CurrentValue + " from node " + this.NodeID + " received");

            if (e.Report.CurrentValue > 0)
            {
                this.Presence = new SensorData(1, "Präsenz");
            }
            else
            {
                this.Presence = new SensorData(0, "Präsenz");
            }

            //if(e.Report.Value > 0) // nur "Presence = ja" beachten
            //{
            //    this._lastPresenceTick = DateTime.Now;
            //    this.UpdatePresence();
            //}
            //this.Presence = new SensorData(e.Report.Value, "Präsenz");
        }

        private void AlarmCmd_Changed(object? sender, ReportEventArgs<SensorAlarmReport> e)
        {
            _logger?.LogDebug("Sensor Alarm " + e.Report.Type + " from node " + this.NodeID + " received");
            this.Alarm = new SensorData(e.Report.Level, (e.Report.Level > 0 ? "🚨" : "") + (e.Report.Level > 0 ? "(" + e.Report.Type.ToString() + " Alarm)" : ""));

        }

        private void SensorCmd_Changed(object? sender, ReportEventArgs<SensorMultiLevelReport> e)
        {
            _logger?.LogDebug("Sensor Data " + e.Report.Type + " from node " + this.NodeID + " received");
            if (e.Report.Type == SensorType.Temperature)
            {
                this.Temperature = new SensorData(e.Report.Value, e.Report.Unit);
            }
            else if (e.Report.Type == SensorType.Luminance)
            {
                this.Luminance = new SensorData(e.Report.Value, e.Report.Unit);
            }
            else if (e.Report.Type == SensorType.RelativeHumidity)
            {
                this.RelativeHumidity = new SensorData(e.Report.Value, e.Report.Unit);
            }
            else if (e.Report.Type == SensorType.DewPoint)
            {
                this.DewPoint = new SensorData(e.Report.Value, "°C");
            }
            else if (e.Report.Type == SensorType.CO2)
            {
                this.CO2 = new SensorData(e.Report.Value, e.Report.Unit);
            }
            else if (e.Report.Type == SensorType.Moisture)
            {
                this.Moisture = new SensorData(e.Report.Value, e.Report.Unit);
            }
            else if (e.Report.Type == (SensorType)39) // 27 == tVOC ???
            {
                this.VolatileOrganicCompounds = new SensorData(e.Report.Value, "ppm");
            }
            else
            {
                _logger?.LogError("UNKNOWN Sensor Data " + e.Report.Type + " from node " + this.NodeID + " received");
                this.Unknown = new SensorData(e.Report.Value, " (" + e.Report.Type.ToString() + " in " + e.Report.Unit + ")");
            }
        }
    }
}
