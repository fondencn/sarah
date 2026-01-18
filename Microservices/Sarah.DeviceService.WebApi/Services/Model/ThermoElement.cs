using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    public class ThermoElement : NetworkElement, IBatterySensor, ITemperatureSensor, IThermoElement
    {
        private SensorData _TemperatureSetpoint;
        private SensorData _Temperature;
        private SensorData _battery;
        private SensorData _basic;
        private readonly NetworkElementPublisher _publisher;


        private Task UpdateTask { get; set; }
        private CancellationTokenSource UpdateCancellationTokenSource { get; set; }

        private IDeviceService _deviceService;


        public override string ClassDescription => "Heizung";

        public override bool? IsActive => this.Basic?.Value > 0;


        public SensorData TemperatureSetpoint
        {
            get { return _TemperatureSetpoint; }
            set
            {
                if (_TemperatureSetpoint != value)
                {
                    _TemperatureSetpoint = value;
                    _publisher.ReportEvent(this, nameof(TemperatureSetpoint), _TemperatureSetpoint?.Value.ToString());
                }
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
                    _publisher.ReportEvent(this, nameof(Temperature), _Temperature?.Value.ToString());
                }
            }
        }



        public SensorData Basic
        {
            get { return _basic; }
            set
            {
                if (_basic != value)
                {
                    _basic = value;
                    _publisher.ReportEvent(this, nameof(Basic), _basic?.Value.ToString());
                }
            }
        }



        public SensorData Battery
        {
            get { return _battery; }
            set
            {
                if (_battery != value)
                {
                    _battery = value;
                    _publisher.ReportEvent(this, nameof(Battery), _battery?.Value.ToString());
                }
            }
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
                    sb.Append("<p>Temperatur: " + this.Temperature.ToString() + "</p>");
                }
                if (this.IsActive == true && !String.IsNullOrWhiteSpace(this.TemperatureSetpoint?.ToString()))
                {
                    sb.Append("<p>Eingestellt: " + this.TemperatureSetpoint.ToString() + "</p>");
                }
                if (this.IsActive == true)
                {
                    sb.Append("<p>Zustand: an</p>");
                }
                else
                {
                    sb.Append("<p>Zustand: aus</p>");
                }
                if (!String.IsNullOrWhiteSpace(this.Battery?.ToString()))
                {
                    sb.Append("<p>Batterie: " + this.Battery.ToString() + "</p>");
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid"></param>
        public ThermoElement(byte nodeid, NetworkElementPublisher publisher, ILogger<ThermoElement>? logger = null) : base(nodeid, logger)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~ThermoElement()
        {
            if (this.UpdateTask != null && this.UpdateTask.Status == TaskStatus.Running)
            {
                this.UpdateCancellationTokenSource.Cancel();
            }
        }

        public override  Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            try
            {
                _logger?.LogDebug("Initializing new Thermo for node " + this.NodeID + " ...");
                this._deviceService = deviceService;
                Node n = deviceService.GetNode(this.NodeID) as Node;


                if (n != null)
                {
                    /* Heizung Basic */
                    Basic basic = n.GetCommandClass<Basic>(); //0=Off 99=LastHeatPoint
                    basic.Changed += OnBasicChanged;


                    /* Heizung Sollwerte */
                    ThermostatSetpoint temperature = n.GetCommandClass<ThermostatSetpoint>();
                    temperature.Changed += OnSetpointChanged;


                    /* Batterie in % */
                    Battery battery = n.GetCommandClass<Battery>();
                    battery.Changed += (sender, e) => this.Battery = new SensorData(e.Report.Value, "%");

                    /* Aktuelle Temperaturmessung */

                    SensorMultiLevel sensorCmd = n.GetCommandClass<SensorMultiLevel>();
                    sensorCmd.Changed += this.SensorCmd_Changed;



                    //global::ZWave.CommandClasses.Version verCmd =  n.GetCommandClass<global::ZWave.CommandClasses.Version>();
                    //VersionReport verReport = await verCmd.Get();
                    //_logger?.LogDebug("ThermoElement " + this.NodeID + " Library Version: " + verReport.Library);
                    //_logger?.LogDebug("ThermoElement " + this.NodeID + " Application Version: " + verReport.Application);
                    //_logger?.LogDebug("ThermoElement " + this.NodeID + " Protocol Version: " + verReport.Protocol);

                    /*
                     * ThermoElement 5 Library Version: 3
                     * ThermoElement 5 Application Version: 4.03
                     * ThermoElement 5 Protocol Version: 4.61
                    */

                    /* Initialwerte holen */
                    try
                    {
                        //BasicReport basicReport = await basic.Get();
                        //this.Basic = new SensorData(basicReport.Value, "");



                        CancellationTokenSource cts = new CancellationTokenSource();
                        this.UpdateCancellationTokenSource = cts;
                        this.UpdateTask = Task.Run(async () =>
                        {
                            while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
                            {
                                /* Alle 30 Minuten */
                                await Task.Delay(30 * 60000);
                                await UpdateSensorData();
                            }
                        }, cts.Token);

                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("ThermoElement::InitializeAsync: ", ex);
            }

            return Task.CompletedTask;
        }

        private async Task UpdateSensorData()
        {
            try
            {
                Node n = this._deviceService.GetNode(this.NodeID) as Node;

                /* Heizung Sollwerte */
                ThermostatSetpoint temperature = n.GetCommandClass<ThermostatSetpoint>();

                /* Batterie in % */
                Battery battery = n.GetCommandClass<Battery>();

                /* Aktuelle Temperaturmessung */
                SensorMultiLevel sensorCmd = n.GetCommandClass<SensorMultiLevel>();

                BatteryReport batReport = await battery.Get();
                this.Battery = new SensorData(batReport.Value, "%");

                ThermostatSetpointReport report = await temperature.Get(ThermostatSetpointType.Heating);
                this.TemperatureSetpoint = new SensorData(report.Value, report.Unit);

                SensorMultiLevelReport sensorReport = await sensorCmd.Get(SensorType.Temperature);
                this.Temperature = new SensorData(sensorReport.Value, sensorReport.Unit);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Error update thermo sensors: " + ex.Message);
            }
        }

        /// <summary>
        /// Setzt die neue Zieltemperatur für das Thermostat
        /// </summary>
        /// <param name="temperature">neue Zieltemperatur in °C</param>
        /// <returns></returns>
        public async Task SetTemperature(float temperature)
        {
            try
            {
                Node node = this._deviceService.GetNode(this.NodeID) as Node;
                var setpointCmd = node.GetCommandClass<ThermostatSetpoint>();
                await setpointCmd.Set(ThermostatSetpointType.Heating, temperature);
                await Task.Delay(5000);
                await UpdateSensorData();
            }
            catch (Exception ex)
            {
                _logger?.LogError("SetTemperature" , ex);
                throw;
            }
        }


        /// <summary>
        /// Setzt die stärke der Heizung (0-99)
        /// 0: Off
        /// 99: LastHeatPoint
        /// </summary>
        /// <param name="level">Heizungsstärke</param>
        /// <returns></returns>
        public async Task SetLevel(byte level)
        {
            try
            {
                Node node = this._deviceService.GetNode(this.NodeID) as Node;
                var basicCmd = node.GetCommandClass<Basic>();
                await basicCmd.Set(level);
                this.SetBasicValue(level); //Fibaro meldet Basic nicht per FLIRS zurück
                await Task.Delay(5000);
                await UpdateSensorData();
            }
            catch (Exception ex)
            {
                _logger?.LogError("SetLevel" , ex);
                throw;
            }
        }

        private void OnSetpointChanged(object sender, ReportEventArgs<ThermostatSetpointReport> e)
        {
            _logger?.LogDebug("ThermostatSetpoint " + e.Report.Value + e.Report.Unit + " event from node " + e.Report.Node.NodeID + " (Scale=" + e.Report.Scale + ", Type=" + e.Report.Type + ")");
            switch (e.Report.Type)
            {
                case ThermostatSetpointType.Heating:
                    this.TemperatureSetpoint = new SensorData(e.Report.Value, e.Report.Unit);
                    SetBasicValue(99);
                    break;
                default:
                    _logger?.LogWarning("WARNING: ThermoElement received unknown setpoint type " + e.Report.Type + " (ignored)!");
                    break;
            }
        }

        private void OnBasicChanged(object sender, ReportEventArgs<BasicReport> e)
        {
            _logger?.LogDebug("Basic report value" + e.Report.CurrentValue + " from node " + e.Report.Node.NodeID);
            SetBasicValue(e.Report.CurrentValue);
        }

        private void SetBasicValue(byte level)
        {
            string unit;
            if (level == 0)
            {
                unit = "❌ aus";
            }
            else if (level == 99)
            {
                unit = "🌡 letzte Einstellung";
            }
            else if (level == 255)
            {
                unit = "🔥 an";
            }
            else
            {
                unit = "?";
            }
            this.Basic = new SensorData(level, unit);
        }

        private void SensorCmd_Changed(object sender, ReportEventArgs<SensorMultiLevelReport> e)
        {
            _logger?.LogDebug("Thermo-Sensor Data " + e.Report.Type + " from node " + this.NodeID + " received");

            switch(e.Report.Type)
            {
                case SensorType.Temperature:
                    this.Temperature = new SensorData(e.Report.Value, e.Report.Unit);
                    break;
                default:
                    _logger?.LogWarning("WARNING: Thermo-Sensor Data with unknown type " + e.Report.Type + " from node " + this.NodeID + " received");
                    break;
            }

        }
    }
}
