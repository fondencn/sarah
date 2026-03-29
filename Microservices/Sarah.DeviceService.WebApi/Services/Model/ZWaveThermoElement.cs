using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.Model.Extensions;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Z-Wave Heizkörperthermostat (TRV).
    /// Kommuniziert über den Z-Wave Controller.
    /// </summary>
    public class ZWaveThermoElement : ThermoElement
    {
        private Task UpdateTask { get; set; }
        private CancellationTokenSource UpdateCancellationTokenSource { get; set; }
        private IDeviceService _deviceService;

        /// <summary>
        /// ctor
        /// </summary>
        public ZWaveThermoElement(byte nodeid, NetworkElementPublisher publisher, ILogger<ZWaveThermoElement>? logger = null) : base(nodeid, publisher, logger)
        {
        }

        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~ZWaveThermoElement()
        {
            if (this.UpdateTask != null && this.UpdateTask.Status == TaskStatus.Running)
            {
                this.UpdateCancellationTokenSource?.Cancel();
            }
        }

        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            try
            {
                _logger?.LogDebug("Initializing new ZWave Thermo for node " + this.NodeID + " ...");
                this._deviceService = deviceService;
                Node? n = deviceService.GetZWaveNode(this.NodeID);

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

                    try
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        this.UpdateCancellationTokenSource = cts;
                        this.UpdateTask = Task.Run(async () =>
                        {
                            while (!cts.Token.IsCancellationRequested)
                            {
                                try
                                {
                                    /* Alle 30 Minuten */
                                    await Task.Delay(30 * 60000, cts.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
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
                _logger?.LogError(ex, "ZWaveThermoElement::InitializeAsync: Error");
            }

            return Task.CompletedTask;
        }

        private async Task UpdateSensorData()
        {
            try
            {
                Node? n = this._deviceService.GetZWaveNode(this.NodeID);
                if (n == null)
                {
                    return;
                }

                ThermostatSetpoint temperature = n.GetCommandClass<ThermostatSetpoint>();
                Battery battery = n.GetCommandClass<Battery>();
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
        public override async Task SetTemperature(float temperature)
        {
            try
            {
                Node? node = this._deviceService.GetZWaveNode(this.NodeID);
                if (node == null)
                {
                    throw new InvalidOperationException($"Node {this.NodeID} not found in ZWave network");
                }
                var setpointCmd = node.GetCommandClass<ThermostatSetpoint>();
                await setpointCmd.Set(ThermostatSetpointType.Heating, temperature);
                await Task.Delay(5000);
                await UpdateSensorData();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ZWaveThermoElement::SetTemperature error");
                throw;
            }
        }

        /// <summary>
        /// Setzt die Stärke der Heizung (0-99): 0=Off, 99=LastHeatPoint
        /// </summary>
        public override async Task SetLevel(byte level)
        {
            try
            {
                Node? node = this._deviceService.GetZWaveNode(this.NodeID);
                if (node == null)
                {
                    throw new InvalidOperationException($"Node {this.NodeID} not found in ZWave network");
                }
                var basicCmd = node.GetCommandClass<Basic>();
                await basicCmd.Set(level);
                this.SetBasicValue(level); //Fibaro meldet Basic nicht per FLIRS zurück
                await Task.Delay(5000);
                await UpdateSensorData();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ZWaveThermoElement::SetLevel error");
                throw;
            }
        }

        private void OnSetpointChanged(object? sender, ReportEventArgs<ThermostatSetpointReport> e)
        {
            _logger?.LogDebug("ThermostatSetpoint " + e.Report.Value + e.Report.Unit + " event from node " + e.Report.Node.NodeID + " (Scale=" + e.Report.Scale + ", Type=" + e.Report.Type + ")");
            switch (e.Report.Type)
            {
                case ThermostatSetpointType.Heating:
                    this.TemperatureSetpoint = new SensorData(e.Report.Value, e.Report.Unit);
                    SetBasicValue(99);
                    break;
                default:
                    _logger?.LogWarning("WARNING: ZWaveThermoElement received unknown setpoint type " + e.Report.Type + " (ignored)!");
                    break;
            }
        }

        private void OnBasicChanged(object? sender, ReportEventArgs<BasicReport> e)
        {
            _logger?.LogDebug("Basic report value " + e.Report.CurrentValue + " from node " + e.Report.Node.NodeID);
            SetBasicValue(e.Report.CurrentValue);
        }

        protected void SetBasicValue(byte level)
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

        private void SensorCmd_Changed(object? sender, ReportEventArgs<SensorMultiLevelReport> e)
        {
            _logger?.LogDebug("Thermo-Sensor Data " + e.Report.Type + " from node " + this.NodeID + " received");

            switch (e.Report.Type)
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
