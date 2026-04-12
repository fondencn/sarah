using Microsoft.Extensions.Configuration;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Abstrakte Basisklasse für alle Thermostat-Heizkörperventile (TRVs).
    /// Enthält gemeinsame Daten und Eigenschaften; konkrete Protokoll-Implementierungen
    /// erben von dieser Klasse (z.B. ZWaveThermoElement, ShellyTrvElement).
    /// </summary>
    public abstract class ThermoElement : NetworkElement, IBatterySensor, ITemperatureSensor, IThermoElement
    {
        private SensorData _TemperatureSetpoint = SensorData.Empty;
        private SensorData _Temperature = SensorData.Empty;
        private SensorData _battery = SensorData.Empty;
        private SensorData _basic = SensorData.Empty;
        protected readonly NetworkElementPublisher _publisher;

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
                    _ = _publisher.ReportEvent(this, nameof(TemperatureSetpoint), _TemperatureSetpoint?.Value.ToString());
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
                    _ = _publisher.ReportEvent(this, nameof(Temperature), _Temperature?.Value.ToString());
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
                    _ = _publisher.ReportEvent(this, nameof(Basic), _basic?.Value.ToString());
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
                    _ = _publisher.ReportEvent(this, nameof(Battery), _battery?.Value.ToString());
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
                if (!String.IsNullOrWhiteSpace(this.TemperatureSetpoint?.ToString()))
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
        protected ThermoElement(byte nodeid, NetworkElementPublisher publisher, ILogger logger) : base(nodeid, logger)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// Setzt die neue Zieltemperatur für das Thermostat
        /// </summary>
        /// <param name="temperature">neue Zieltemperatur in °C</param>
        public abstract Task SetTemperature(float temperature);

        /// <summary>
        /// Setzt die Stärke der Heizung (0-99)
        /// 0: Off, 99: LastHeatPoint
        /// </summary>
        public abstract Task SetLevel(byte level);
    }
}
