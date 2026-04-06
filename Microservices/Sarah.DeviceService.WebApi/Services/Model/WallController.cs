using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.Model.Extensions;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    public class WallController : NetworkElement, IBatterySensor, IWallController
    {

        private SensorData _battery;
        private readonly NetworkElementPublisher _publisher;

        /// <summary>
        /// Zeitpunkt der Letzten Aktivierung
        /// </summary>
        public DateTime? LastUsage { get; private set; }
        /// <summary>
        /// Zuletzt gesendete ScenenId
        /// </summary>
        public byte LastSceneId { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public override string ClassDescription => "Wandschalter";

        /// <summary>
        /// Zustandsdaten für die Anzeige
        /// </summary>
        public override string StateInfo => "Letzte Aktivität: " + (LastUsage.HasValue ? LastUsage.Value.ToString() : "Unbekannt") + ", SceneId: " + LastSceneId + ", Batterie: " + Battery;
        public SensorData Battery { get => _battery; private set { if (_battery != value) { _battery = value; _publisher.ReportEvent(this, nameof(Battery), value?.ToString()); } } }


        public WallController(byte nodeid, NetworkElementPublisher publisher, ILogger<WallController>? logger = null) : base(nodeid, logger)
        {
            _publisher = publisher;
        }


        /// <summary>
        /// Initialisiert die Verbindung zum ZWave Gerät
        /// </summary>
        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            Node? node = deviceService.GetZWaveNode(this.NodeID);
            _logger?.LogDebug("Initialize Node " + this.NodeID + " as " +  this.Name );

            if (node != null)
            {
                try
                {
                    var centralScne = node.GetCommandClass<CentralScene>();
                    centralScne.Changed += OnCentralSceneChanged;


                    Battery battery = node.GetCommandClass<Battery>();
                    battery.Changed += (sender, e) => this.Battery = new SensorData(e.Report.Value, "%");


                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex.Message);
                }


                //try
                //{

                //    Configuration config = node.GetCommandClass<Configuration>();
                //    ConfigurationReport cr = await config.Get(11);
                //    byte valueOfParameter11 = cr.Parameter;
                //}
                //catch (Exception ex)
                //{

                //    _logger?.LogDebug(ex.Message);
                //}

                //try
                //{

                //    Battery battery = node.GetCommandClass<Battery>();
                //    var batteryReport = await battery.Get();
                //    this.BatteryLevel = batteryReport.Value;
                //}
                //catch (Exception ex)
                //{

                //    _logger?.LogDebug(ex.Message);
                //}


                //try
                //{

                //    var switchBin = node.GetCommandClass<SwitchBinary>();
                //    var switchBinReport = await switchBin.Get();
                //    _logger?.LogDebug("switchBinReport.Value: " + switchBinReport.Value);
                //}
                //catch (Exception ex)
                //{
                //    _logger?.LogDebug(ex.Message);
                //}
            }
            return Task.CompletedTask;
        }


        private Task OnClicked()
        {
            _ = _publisher.ReportClickedEvent(this, this.LastSceneId);
            return Task.CompletedTask;
        }

        private async void OnCentralSceneChanged(object sender, ReportEventArgs<CentralSceneReport> e)
        {
            _logger?.LogDebug($"CentralScene report of Node {e.Report.Node:D3} changed to [{e.Report}]");

            this.LastUsage = DateTime.Now;
            this.LastSceneId = e.Report.SceneNumber;
            await this.OnClicked();
        }

    }
}
