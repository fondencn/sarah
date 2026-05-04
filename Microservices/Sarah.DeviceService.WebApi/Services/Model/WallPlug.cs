using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.Model.Extensions;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Steckdose
    /// </summary>
    public abstract class WallPlug : NetworkElement, IWallPlug
    {
        private bool _isOn;
        private SensorData _meter_kwh = new SensorData(0, "kWh");
        private SensorData _meter_kVAh = new SensorData(0, "kVAh");
        private SensorData _meter_W = new SensorData(0, "W");
        private SensorData _meter_A = new SensorData(0, "A");
        private DateTime _lastStateChange;
        protected readonly NetworkElementPublisher _publisher;

        protected WallPlug(byte nodeid, NetworkElementPublisher publisher, ILogger logger) : base(nodeid, logger)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// Schaltet die Steckdose an oder aus
        /// </summary>
        /// <param name="newState">der zu setzende Zustand (true bedeutet an)</param>
        /// <returns>Task</returns>
        public abstract Task SetState(bool newState);

        private float PowerHighThreshold { get; } = 20; // 20 W veränderung bedeutet: Jemand hat was angemacht
        private float PowerLowThreshold { get; } = -20; // -20 W veränderung bedeutet: Jemand hat was ausgemacht

        /// <summary>
        /// ClassDescription
        /// </summary>
        public override string ClassDescription => "Steckdose";

        /// <summary>
        /// Zeitpunkt, zu dem die Steckdose zuletzt von an auf aus oder umgekehrt gestellt wurde
        /// </summary>
        public DateTime LastStateChange => _lastStateChange;

        /// <summary>
        /// Status des Schalters (an oder aus)
        /// </summary>
        public bool IsOn { get => _isOn; protected set { if (this._isOn != value) { this._isOn = value; this._lastStateChange = DateTime.Now; _ = _publisher.ReportEvent(this, nameof(IsOn), value); _ = _publisher.ReportWallPlugStateChanged(this, value, this.LastChangeToPowerLow, this.LastChangeToPowerHigh); } } }

        /// <summary>
        /// Meter in Kilowattstunden
        /// </summary>
        public SensorData Meter_kWh { get => _meter_kwh; protected set { if (_meter_kwh != value) { _meter_kwh = value; _ = _publisher.ReportEvent(this, nameof(Meter_kWh), value?.ToString()); } } }

        /// <summary>
        /// Meter in 1000 Volt-Ampère-Stunden
        /// </summary>
        public SensorData Meter_kVAh { get => _meter_kVAh; protected set { if (_meter_kVAh != value) { _meter_kVAh = value; _ = _publisher.ReportEvent(this, nameof(Meter_kVAh), value?.ToString()); } } }

        /// <summary>
        /// Meter in Watt
        /// </summary>
        public SensorData Meter_W
        {
            get => _meter_W;
            protected set
            {
                float oldVal = _meter_W.Value;
                float newVal = value.Value;

                bool reportChanges = false;

                if (oldVal != newVal)
                {
                    if (oldVal > PowerLowThreshold && newVal <= PowerLowThreshold)
                    {
                        this.LastChangeToPowerLow = DateTime.Now;
                        reportChanges = true;
                    }
                    else if (oldVal <= PowerHighThreshold && newVal > PowerHighThreshold)
                    {
                        this.LastChangeToPowerHigh = DateTime.Now;
                        reportChanges = true;
                    }
                    if (reportChanges)
                    {
                        _ = _publisher.ReportWallPlugStateChanged(this, this.IsOn, this.LastChangeToPowerLow, this.LastChangeToPowerHigh);
                    }
                }
                _meter_W = value;
                _ = _publisher.ReportEvent(this, nameof(Meter_W), value?.ToString());
            }
        }

        /// <summary>
        /// Meter in Ampère
        /// </summary>
        public SensorData Meter_A { get => _meter_A; protected set { if (_meter_A != value) { _meter_A = value; _ = _publisher.ReportEvent(this, nameof(Meter_A), value?.ToString()); } } }


        /// <summary>
        /// Zeitpunkt, zu welchem die anliegende Leistung zuletzt abgesunken ist
        /// </summary>
        public DateTime LastChangeToPowerLow { get; private set; }

        /// <summary>
        /// Zeitpunkt, zu welchem die anliegende Leistung zuletzt angestiegen ist
        /// </summary>
        public DateTime LastChangeToPowerHigh { get; private set; }

        /// <summary>
        /// Gibt an, ob das Gerät gerade eingeschaltget ist
        /// </summary>
        public override bool? IsActive => this.IsOn;


        protected DateTime LastMeterReport { get; set; }


        /// <summary>
        /// Schaltet zwischen an und aus um
        /// </summary>
        public void ToggleState() => this.SetState(!this.IsOn);

        /// <summary>
        ///
        /// </summary>
        public override string StateInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append((this.IsOn == true ? "Zustand: ✔ An" : "⏹️ Aus") + " | ");
                if (!String.IsNullOrWhiteSpace(this.Meter_kWh?.ToString()))
                {
                    sb.Append("Verbrauch: " + this.Meter_kWh?.ToString() + " | ");
                }
                if (!String.IsNullOrWhiteSpace(this.Meter_W?.ToString()))
                {
                    sb.Append("Leistung: " + this.Meter_W?.ToString() + " | ");
                }
                if (!String.IsNullOrWhiteSpace(this.Meter_kVAh?.ToString()))
                {
                    sb.Append("Strom: " + this.Meter_kVAh?.ToString() + " | ");
                }

                if (this.LastMeterReport != DateTime.MinValue)
                {
                    sb.Append("Stand: " + this.LastMeterReport);
                }
                return sb.ToString();
            }
        }
    }




/// <summary>
/// ZWAVE Steckdose
/// </summary>
public class ZWaveWallPlug : WallPlug
    {
        private Task? _updateSensorDataTask;
        private CancellationTokenSource? _UpdateSensorDataCancellationTokenSource;
        private IDeviceService _deviceService;


        /// <summary>
        /// Macht die Steckdose an oder aus.
        /// </summary>
        /// <param name="newState"></param>
        /// <returns></returns>
        public override async Task SetState(bool newState)
        {

            try
            {
                Node? node = _deviceService.GetZWaveNode(this.NodeID);
                if (node == null)
                {
                    _logger?.LogWarning("SetState: Node {NodeId} not available", this.NodeID);
                    return;
                }

                var switchBin = node.GetCommandClass<SwitchBinary>();
                await switchBin.Set(newState);
                _logger?.LogDebug("switchBinReport.Value SET To: " + newState);

                /* Zustand überprüfen */
                var switchBinReport = await switchBin.Get();
                _logger?.LogDebug("switchBinReport.Value: " + switchBinReport.CurrentValue);
                this.IsOn = switchBinReport.CurrentValue.GetValueOrDefault(false);

                /* Anliegende Leistung abfragen */
                await UpdateSensorData();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex.Message);
            }


        }

        /// <summary>
        /// Pollt den Meter-Status des Knotens
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSensorData()
        {
            try
            {
                Node? node = _deviceService.GetZWaveNode(this.NodeID);
                if (node == null)
                {
                    _logger?.LogWarning("UpdateSensorData: Node {NodeId} not available", this.NodeID);
                    return;
                }

                var meter = node.GetCommandClass<Meter>();
                var meterReport = await meter.Get(ElectricMeterScale.kWh);
                if (meterReport != null)
                {
                    _logger?.LogDebug("meterReport.Value: " + meterReport.Value + meterReport.Unit + " (Type " + meterReport.Type + ")");
                    this.SetMeter(new SensorData(meterReport.Value, meterReport.Unit));
                }

                meter = node.GetCommandClass<Meter>();
                meterReport = await meter.Get(ElectricMeterScale.W);
                if (meterReport != null)
                {
                    _logger?.LogDebug("meterReport.Value: " + meterReport.Value + meterReport.Unit + " (Type " + meterReport.Type + ")");
                    this.SetMeter(new SensorData(meterReport.Value, meterReport.Unit));
                }

                //meterReport = await meter.Get(ElectricMeterScale.kVAh);
                //_logger?.LogDebug("meterReport.Value: " + meterReport.Value + meterReport.Unit + " (Type " + meterReport.Type + ")");
                //this.SetMeter(new SensorData(meterReport.Value, meterReport.Unit));



                if (this.NodeID == 20) // kann nur die Aeotec Steckdose, Popp und Fibaro nicht
                {
                    meter = node.GetCommandClass<Meter>();
                    meterReport = await meter.Get(ElectricMeterScale.A);
                    if (meterReport != null)
                    {
                        _logger?.LogDebug("meterReport.Value: " + meterReport.Value + meterReport.Unit + " (Type " + meterReport.Type + ")");
                        this.SetMeter(new SensorData(meterReport.Value, meterReport.Unit));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Fehler beim Abfragen des Wallplug-Meters für Node " + this.NodeID + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Setzt die entsprechende Meter-Eigenschaft abhängig von der Einheit des gemeldeten Werts
        /// </summary>
        /// <param name="sensorData"></param>
        private void SetMeter(SensorData sensorData)
        {
            if (sensorData.Unit.Equals("kWh", StringComparison.OrdinalIgnoreCase))
            {
                /* bei kWh interessieren die tausendstel nicht */
                sensorData = new SensorData((float)Math.Round(sensorData.Value, 1), sensorData.Unit);
                this.Meter_kWh = sensorData;
            }
            else if (sensorData.Unit.Equals("kVAh", StringComparison.OrdinalIgnoreCase))
            {
                this.Meter_kVAh = sensorData;
            }
            else if (sensorData.Unit.Equals("W", StringComparison.OrdinalIgnoreCase))
            {
                this.Meter_W = sensorData;
            }
            else if (sensorData.Unit.Equals("A", StringComparison.OrdinalIgnoreCase))
            {
                this.Meter_A = sensorData;
            }
            else
            {
                _logger?.LogDebug("WallPlug: Unbekannter Leistungswert " + sensorData.Unit);
            }
            
            LastMeterReport = DateTime.Now;
        }


        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid">ID des ZWave Knotens</param>
        public ZWaveWallPlug(byte nodeid, NetworkElementPublisher publisher, ILogger<ZWaveWallPlug> logger, IDeviceService deviceService) : base(nodeid, publisher, logger)
        {
            this._deviceService = deviceService;
        }


        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~ZWaveWallPlug()
        {
            if (this._updateSensorDataTask != null && _UpdateSensorDataCancellationTokenSource != null && this._updateSensorDataTask.Status == TaskStatus.Running)
            {
                this._UpdateSensorDataCancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Initialisiert die Verbindung zum ZWave Gerät
        /// </summary>
        public override async Task InitializeAsync(IDeviceService deviceService, IConfiguration config)
        {
            this._deviceService = deviceService;
            Node? node = deviceService.GetZWaveNode(this.NodeID);
            _logger?.LogDebug("Initialize Node " + this.NodeID + " as " + this.Name);

            if (node != null)
            {
                try
                {
                    var basic = node.GetCommandClass<Basic>();
                    basic.Changed += OnBasicChanged;


                    var switchBin = node.GetCommandClass<SwitchBinary>();
                    switchBin.Changed += OnSwitchBinChanged;


                    var switchMulti = node.GetCommandClass<SwitchMultiLevel>();
                    switchMulti.Changed += OnSwitchMultiChanged;


                    var meter = node.GetCommandClass<Meter>();
                    meter.Changed += OnMeterChanged;

                    node.UnknownCommandReceived += this.Node_UnknownCommandReceived;
                    node.MessageReceived += this.Node_MessageReceived;


                    //if(NodeID == 10)
                    //{
                    //    Association ass = node.GetCommandClass<Association>();
                    //    var groupone = await ass.Get(1);
                    //    await ass.Add(1, 1);
                    //    await ass.Add(2, 1);
                    //    await ass.Add(3, 1);

                    //    groupone = await ass.Get(1);
                    //}

                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex.Message);
                }

                try
                {

                    var switchBin = node.GetCommandClass<SwitchBinary>();
                    var switchBinReport = await switchBin.Get();
                    _logger?.LogDebug("switchBinReport.Value: " + switchBinReport.CurrentValue);
                    this.IsOn = switchBinReport.CurrentValue.GetValueOrDefault(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex.Message);
                }

                CancellationTokenSource cts = new CancellationTokenSource();
                this._UpdateSensorDataCancellationTokenSource = cts;
                this._updateSensorDataTask = Task.Run(async () =>
                {
                    await Task.Delay(2 * 60 * 1000); // 2 MInuten warten, damit der Programmstart nicht durcheinander kommt

                    while (!this._UpdateSensorDataCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        bool isDeviceStateLatelyChanged = (DateTime.Now - this.LastStateChange).TotalMinutes < 10; // 10 Minuten
                        bool isSensorDataOutOfDate = (DateTime.Now - this.LastMeterReport).TotalMinutes > 10;      // 10 Minuten

                        if (isSensorDataOutOfDate || isDeviceStateLatelyChanged) // Abfrage nur wenn auch Sinnvoll (Timer Ticket aber trotzdem jede Minute)
                        {
                            await this.UpdateSensorData();
                        }
                        await Task.Delay(1 * 60 * 1000); // Check jede Minute
                    }
                }, cts.Token);
            }
        }

        private void OnMeterChanged(object? sender, ReportEventArgs<MeterReport> e)
        {
            _logger?.LogDebug($"Meter report of Node {e.Report.Node:D3} changed to [{e.Report.Value}{e.Report.Unit}] ({e.Report.Type})");
            this.SetMeter(new SensorData(e.Report.Value, e.Report.Unit));
        }

        private void Node_MessageReceived(object? sender, EventArgs e)
        {
            //_logger?.LogDebug("Node " + this.NodeID + " Message received");
        }

        private void Node_UnknownCommandReceived(object? sender, global::ZWave.Channel.NodeEventArgs e)
        {
            _logger?.LogWarning("Node " + this.NodeID + " UNKNOWN COMMAND received");

        }

        private void OnSwitchBinChanged(object? sender, ReportEventArgs<SwitchBinaryReport> e)
        {
            _logger?.LogDebug($"SwitchMultiLevel report of Node {e.Report.Node:D3} changed to [{e.Report}]");
            this.IsOn = e.Report.CurrentValue.GetValueOrDefault(false);
        }



        private void OnBasicChanged(object? sender, ReportEventArgs<BasicReport> e)
        {
            _logger?.LogDebug($"Basic report of Node {e.Report.Node:D3} changed to [{e.Report}]");
            /* Für Popp Wallcontroller kmmt hier eine Basic Notification an die Association Group 1 (Lifeline) */
            this.IsOn = e.Report.CurrentValue != 0;
        }
        private void OnSwitchMultiChanged(object? sender, ReportEventArgs<SwitchMultiLevelReport> e)
        {
            _logger?.LogDebug($"SwitchMultiLevel report of Node {e.Report.Node:D3} changed to [{e.Report}]");
            this.IsOn = e.Report.CurrentValue != 0;
        }

    }
}
