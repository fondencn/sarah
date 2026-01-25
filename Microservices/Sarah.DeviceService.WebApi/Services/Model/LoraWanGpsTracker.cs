using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Microsoft.Extensions.Logging;
using Sarah.Ttn;
using Sarah.DeviceService.WebApi.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// Ein LoRaWan GPS Tracker
    /// </summary>
    public class LoraWanGpsTracker : NetworkElement, IDisposable, IGPSTracker, IBatterySensor
    {
        private readonly NetworkElementPublisher _publisher;
        private string Ttn_cf_ApiKey {get; set;}
        private string TtnApiKey_SarahApiKey {get; set; }
        private const string TtnAppName = "sarah-lorawan";
        private const string TtnUserName = "sarah-lorawan@ttn";
        private const string TtnHostname = "eu1.cloud.thethings.network";
        private const int TtnPort = 8883;
        private readonly TtnClient _ttn;
        private SensorData? _battery = null;
        private LocatorPosition? _position = null;
        private SensorData _isButtonPressed;
        private const int MAX_POSITION_TRACE_ENTRIES = 50;
        private readonly Queue<LocatorPosition> _PositionTrace = new Queue<LocatorPosition>();

        /// <summary>
        /// Positionsverlauf dieses Objektes
        /// </summary>
        public LocatorPosition[] PositionTrace => _PositionTrace.ToArray();

        /// <summary>
        /// 
        /// </summary>
        public bool IsTtnConnected { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime LastMessageReceived { get; private set; }

        /// <summary>
        /// Liste aller MACAdressen in der Nähe des Trackers, die über Bluetooth oder Wifi aufgezeichnet wurden.
        /// </summary>
        public List<TTNTrackerNearbyDevice> NearbyDevices { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        private string TtnDeviceId { get; }

        /// <summary>
        /// Batterieladezustand in %
        /// </summary>
        public SensorData Battery
        {
            get { return _battery ?? SensorData.Empty; }
            set
            {
                if (_battery != value)
                {
                    _battery = value;
                    _ = _publisher.ReportEvent(this, nameof(Battery), _battery?.Value.ToString(CultureInfo.CurrentCulture));
                }
            }
        }

        /// <summary>
        /// Gibt an, ob der Button auf dem Tracker beim letzten Event gedrückt wurde
        /// </summary>
        public SensorData IsButtonPressed
        {
            get { return _isButtonPressed; }
            set
            {
                if (_isButtonPressed != value)
                {
                    _isButtonPressed = value;
                    _ = _publisher.ReportEvent(this, nameof(IsButtonPressed), _isButtonPressed?.Value.ToString(CultureInfo.CurrentCulture));
                }
            }
        }

        /// <summary>
        /// Die Position des Objektes im Georaum
        /// </summary>
        public LocatorPosition Position
        {
            get { return _position ?? LocatorPosition.Empty; }
            private set
            {
                if (_position != value)
                {
                    _position = value;
                    _ = _publisher.ReportEvent(this, "Longtitude", _position?.Longtitude?.Value.ToString(CultureInfo.CurrentCulture));
                    _ = _publisher.ReportEvent(this, "Latitude", _position?.Latitude?.Value.ToString(CultureInfo.CurrentCulture));

                    if (value.IsValid)
                    {
                        AddToTrace(value);
                        LastValidPosition = value;
                    }
                }
            }
        }

        public LocatorPosition LastValidPosition { get; private set; }

        private void AddToTrace(LocatorPosition value)
        {
            this._PositionTrace.Enqueue(value);
            if (this._PositionTrace.Count > MAX_POSITION_TRACE_ENTRIES)
            {
                this._PositionTrace.Dequeue();
            }
        }

        public override string StateInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Letzte Nachricht: " + (this.LastMessageReceived == DateTime.MinValue ? "Keine" : this.LastMessageReceived.ToString("dd.MM.yy HH:mm:ss")) + " | ");
                sb.Append("Aktuelle Position: " + this.Position + " | ");
                sb.Append("Letzte bekannte Position: " + this.LastValidPosition + " | ");
                sb.Append("Entfernung: " + (this.LastValidPosition == null ? "Unbekannt" : (Math.Round(this.LastValidPosition.GetDistanceTo(LocatorPosition.ZuHause), 2).ToString() + " m")) + " | ");
                sb.Append("Batterie: " + this.Battery + " | ");
                sb.Append("SOS-Knopf gedrückt: " + this.IsButtonPressed + " | ");
                if (this.NearbyDevices?.Any() == true)
                {
                    sb.Append("Geräte in der Nähe: " + String.Join(",", this.NearbyDevices.Select(item => item.ToString())) + " | ");
                }
                return sb.ToString();
            }
        }

        private ITTNPayloadParser Parser { get; }

        public LoraWanGpsTracker(byte nodeid, string ttnDeviceId, NetworkElementPublisher publisher, ILogger<LoraWanGpsTracker>? logger = null) : base(nodeid, logger)
        {
            _publisher = publisher;
            this.TtnDeviceId = ttnDeviceId;
            this.Parser = new SenseCapTTNParser(); // TODO: Make configurable if more devices get integrated
            this._ttn = new TtnClient(TtnAppName);
            this._ttn.MessageReceived += OnTtnMessageReceived;
            this._ttn.ConnectionStateChanged += OnTtnConnectionChanged;
        }

        private void OnTtnConnectionChanged(object sender, bool e)
        {
            if (e)
            {
                _logger?.LogDebug("TTN Connected: Device " + this.TtnDeviceId);
                this.IsTtnConnected = true;
            }
            else
            {
                _logger?.LogDebug("TTN Disconnected: Device " + this.TtnDeviceId);
                this.IsTtnConnected = false;
            }
        }

        private void OnTtnMessageReceived(string topic, TtnMessage msg)
        {
            if (topic.Contains(this.TtnDeviceId)) // nur auf eigene Messages hören
            {
                _logger?.LogDebug("OnTtnMessageReceived Topic" + topic);
                if (msg.uplink_message != null && msg.uplink_message.frm_payload != null)
                {
                    string payloadBase64 = msg.uplink_message.frm_payload;
                    var deserializedDeviceData = this.Parser.Parse(payloadBase64);
                    _logger?.LogDebug("OnTtnMessageReceived DataId" + deserializedDeviceData.DataId);

                    if(deserializedDeviceData != null)
                    {
                        this.Battery = deserializedDeviceData.Battery;
                    }
                    if(deserializedDeviceData?.Longitude != null && deserializedDeviceData.Latitude != null) 
                    { 
                        var locatorPos = new LocatorPosition(deserializedDeviceData.Longitude, deserializedDeviceData.Latitude);
                        if(locatorPos.IsValid)
                        {
                            this.Position = locatorPos;
                        }
                    }
                    this.LastMessageReceived = DateTime.Now;
                    this.IsButtonPressed = new SensorData(deserializedDeviceData?.IsButtonSosEvent == true ? 1f : 0f, "");
                    if (deserializedDeviceData?.NearbyDevices?.Any() == true)
                    {
                        this.NearbyDevices = deserializedDeviceData.NearbyDevices;
                    }
                }
            }
        }

        public override async Task InitializeAsync(IDeviceService deviceService, IConfiguration config)
        {
            LoadTtnApiKey(config);
            await _ttn.Start(TtnHostname, TtnPort, true, TtnUserName, TtnApiKey_SarahApiKey, TimeSpan.FromSeconds(5));
        }

        private  void LoadTtnApiKey(IConfiguration config) 
        {
            Ttn_cf_ApiKey = config["TTN:AppApiKey"] ?? throw new InvalidOperationException("TTN:AppApiKey not configured in DeviceService configuration");
            TtnApiKey_SarahApiKey = config["TTN:SarahApiKey"] ?? throw new InvalidOperationException("TTN:SarahApiKey not configured in DeviceService configuration");
        }

        public void Dispose()
        {
            this._ttn.Dispose();
        }
    }

    public interface ITTNPayloadParser
    {
        TTNTrackerPayload Parse(string payloadBase64);
    }

    public class SenseCapTTNParser : ITTNPayloadParser
    {



        public TTNTrackerPayload Parse(string payloadBase64)
        {
            byte[] payloadBytes = Convert.FromBase64String(payloadBase64);
            DataId dataId = (DataId)payloadBytes[0];


            TTNTrackerPayload decoded = new TTNTrackerPayload();
            decoded.DataId = dataId;
            decoded.RawData = payloadBase64;

            switch (dataId)
            {
                case DataId.DeviceStatus_EventMode:
                    decoded.Battery = new SensorData(payloadBytes[1], "%");
                    decoded.Timestamp = DateTime.Now;
                    break;
                case DataId.DeviceStatus_PeriodicMode:
                    decoded.Battery = new SensorData(payloadBytes[1], "%");
                    decoded.Timestamp = DateTime.Now;
                    break;
                case DataId.Heatbeat:
                    decoded.Battery = new SensorData(payloadBytes[1], "%");
                    decoded.Timestamp = DateTime.Now;
                    break;
                case DataId.Location_GNSS_Only:
                    {
                        byte[] eventstatus = new byte[4]
                            { 0, payloadBytes[1], payloadBytes[2],payloadBytes[3]}.Reverse().ToArray();

                        byte motionsegmentnumber = payloadBytes[4];
                        byte[] utctime = payloadBytes.Skip(5).Take(4).Reverse().ToArray();
                        byte[] longitude = payloadBytes.Skip(9).Take(4).Reverse().ToArray();
                        byte[] latitude = payloadBytes.Skip(13).Take(4).Reverse().ToArray();
                        byte battery = payloadBytes[17];

                        decoded.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
                            .AddSeconds(BitConverter.ToInt32(utctime))
                            .ToLocalTime();
                        decoded.Latitude = new SensorData(BitConverter.ToInt32(latitude) / 1000000.0f, "°");
                        decoded.Longitude = new SensorData(BitConverter.ToInt32(longitude) / 1000000.0f, "°");
                        decoded.Battery = new SensorData(battery, "%");

                        decoded.IsStartMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x01) != 0;
                        decoded.IsEndMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x02) != 0;
                        decoded.IsNoMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x04) != 0;
                        decoded.IsButtonSosEvent = (BitConverter.ToInt32(eventstatus) & 0x40) != 0;
                        decoded.IsButtonEvent = (BitConverter.ToInt32(eventstatus) & 0x80) != 0;
                        decoded.Message = $"StartMove: {decoded.IsStartMoveEvent}; EndMove: {decoded.IsEndMoveEvent}; NoMove: {decoded.IsNoMoveEvent}; ButtonPressed: {decoded.IsButtonEvent}; SOS: {decoded.IsButtonSosEvent}";
                    }
                    break;
                case DataId.Location_Wifi_Only:
                    {
                        byte[] eventstatus = new byte[4]
                            { 0, payloadBytes[1], payloadBytes[2],payloadBytes[3]}.Reverse().ToArray();
                        byte motionsegmentnumber = payloadBytes[4];
                        byte[] utctime = payloadBytes.Skip(5).Take(4).Reverse().ToArray();
                        byte[] mac1 = payloadBytes.Skip(9).Take(6).Reverse().ToArray();
                        byte rssi1 = payloadBytes[15];
                        byte[] mac2 = payloadBytes.Skip(17).Take(6).Reverse().ToArray();
                        byte rssi2 = payloadBytes[22];
                        byte[] mac3 = payloadBytes.Skip(24).Take(6).Reverse().ToArray();
                        byte rssi3 = payloadBytes[29];
                        byte[] mac4 = payloadBytes.Skip(30).Take(6).Reverse().ToArray();
                        byte rssi4 = payloadBytes[36];
                        byte battery = payloadBytes[37];

                        decoded.NearbyDevices.Add(new TTNTrackerNearbyDevice() { MAC = mac1, RSSI = rssi1 });
                        decoded.NearbyDevices.Add(new TTNTrackerNearbyDevice() { MAC = mac2, RSSI = rssi2 });
                        decoded.NearbyDevices.Add(new TTNTrackerNearbyDevice() { MAC = mac3, RSSI = rssi3 });
                        decoded.NearbyDevices.Add(new TTNTrackerNearbyDevice() { MAC = mac4, RSSI = rssi4 });
                        decoded.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
                            .AddSeconds(BitConverter.ToInt32(utctime))
                            .ToLocalTime();
                        decoded.Battery = new SensorData(battery, "%");

                        decoded.IsStartMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x01) != 0;
                        decoded.IsEndMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x02) != 0;
                        decoded.IsNoMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x04) != 0;
                        decoded.IsButtonSosEvent = (BitConverter.ToInt32(eventstatus) & 0x40) != 0;
                        decoded.IsButtonEvent = (BitConverter.ToInt32(eventstatus) & 0x80) != 0;
                        decoded.Message = $"StartMove: {decoded.IsStartMoveEvent}; EndMove: {decoded.IsEndMoveEvent}; NoMove: {decoded.IsNoMoveEvent}; ButtonPressed: {decoded.IsButtonEvent}; SOS: {decoded.IsButtonSosEvent}";
                    }
                    break;
                case DataId.Location_Bluetooth_Only:
                    {
                        byte[] eventstatus = new byte[4]
                            { 0, payloadBytes[1], payloadBytes[2],payloadBytes[3]}.Reverse().ToArray();
                        byte motionsegmentnumber = payloadBytes[4];
                        byte[] utctime = payloadBytes.Skip(5).Take(4).Reverse().ToArray();
                        byte[] mac1 = payloadBytes.Skip(9).Take(6).Reverse().ToArray();
                        byte rssi1 = payloadBytes[15];
                        byte[] mac2 = payloadBytes.Skip(17).Take(6).Reverse().ToArray();
                        byte rssi2 = payloadBytes[22];
                        byte[] mac3 = payloadBytes.Skip(24).Take(6).Reverse().ToArray();
                        byte rssi3 = payloadBytes[29];
                        byte battery = payloadBytes[30];

                        decoded.NearbyDevices.Add(new TTNTrackerNearbyDevice() { MAC = mac1, RSSI = rssi1 });
                        decoded.NearbyDevices.Add(new TTNTrackerNearbyDevice() { MAC = mac2, RSSI = rssi2 });
                        decoded.NearbyDevices.Add(new TTNTrackerNearbyDevice() { MAC = mac3, RSSI = rssi3 });
                        decoded.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
                            .AddSeconds(BitConverter.ToInt32(utctime))
                            .ToLocalTime();
                        decoded.Battery = new SensorData(battery, "%");

                        decoded.IsStartMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x01) != 0;
                        decoded.IsEndMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x02) != 0;
                        decoded.IsNoMoveEvent = (BitConverter.ToInt32(eventstatus) & 0x04) != 0;
                        decoded.IsButtonSosEvent = (BitConverter.ToInt32(eventstatus) & 0x40) != 0;
                        decoded.IsButtonEvent = (BitConverter.ToInt32(eventstatus) & 0x80) != 0;
                        decoded.Message = $"StartMove: {decoded.IsStartMoveEvent}; EndMove: {decoded.IsEndMoveEvent}; NoMove: {decoded.IsNoMoveEvent}; ButtonPressed: {decoded.IsButtonEvent}; SOS: {decoded.IsButtonSosEvent}";
                    }
                    break;
                case DataId.Positioning_Timeout_and_ErrorCode:
                    // Int32-ErrorCode ab Byteposition 1:
                    byte[] errorCodeBytes = new byte[payloadBytes.Length - 1];
                    errorCodeBytes[0] = payloadBytes[1];
                    errorCodeBytes[1] = payloadBytes[2];
                    errorCodeBytes[2] = payloadBytes[3];
                    errorCodeBytes[3] = payloadBytes[4];

                    decoded.Timestamp = DateTime.Now;
                    errorCodeBytes = errorCodeBytes.Reverse().ToArray();
                    int errorCode = BitConverter.ToInt32(errorCodeBytes);
                    decoded.Message = CreateErrorMessage(errorCode);
                    break;
                case DataId.Location_GNSS_Sensor_Battery:
                    decoded.Timestamp = DateTime.Now;
                    decoded.Message = "SenseCapTTNParser Model B hat keinen Sensor -> Nachricht Location_GNSS_Sensor_Battery dürfte eigentlich nicht ankommen";
                    break;
                case DataId.Location_Wifi_Sensor:
                    decoded.Timestamp = DateTime.Now;
                    decoded.Message = "SenseCapTTNParser Model B hat keinen Sensor -> Nachricht Location_Wifi_Sensor dürfte eigentlich nicht ankommen";
                    break;
                case DataId.Location_Bluetooth_Sensor:
                    decoded.Timestamp = DateTime.Now;
                    decoded.Message = "SenseCapTTNParser Model B hat keinen Sensor -> Nachricht Location_Bluetooth_Sensor dürfte eigentlich nicht ankommen";
                    break;
                default:
                    decoded.Timestamp = DateTime.Now;
                    decoded.Message = "SenseCapTTNParser hat Unbekannte Nachricht von TTN empfangen";
                    break;
            }

            return decoded;
        }

        private string CreateErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 0:
                    return "The GNSS scan timed out and failed to obtain the location";
                case 1:
                    return "The Wi-Fi scan timed out and failed to obtain the location.";
                case 2:
                    return "The Wi-Fi + GNSS scan timed out and failed to obtain the location.";
                case 3:
                    return "The GNSS + Wi-Fi scan timed out and failed to obtain the location.";
                case 4:
                    return "The Bluetooth scan timed out and failed to obtain the location.";
                case 5:
                    return "The Bluetooth + Wi-Fi scan timed out and failed to obtain the location.";
                case 6:
                    return "The Bluetooth + GNSS scan timed out and failed to obtain the location.";
                case 7:
                    return "The Bluetooth + Wi-Fi + GNSS scan timed out and failed to obtain the \r\nlocation.";
                default:
                    return "Unbekannter Fehlercode " + errorCode + " beim lesen der Positioning_Timeout_and_ErrorCode Nachricht";
            }
        }
    }

    public class TTNTrackerPayload
    {
        public DataId DataId { get; set; }
        public SensorData Battery { get; set; }
        public SensorData Longitude { get; set; }
        public SensorData Latitude { get; set; }
        public SensorData ButtonState { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string RawData { get; set; }
        public List<TTNTrackerNearbyDevice> NearbyDevices { get; } = new List<TTNTrackerNearbyDevice>();
        public bool IsStartMoveEvent { get; set; }
        public bool IsEndMoveEvent { get; set; }
        public bool IsNoMoveEvent { get; set; }
        public bool IsButtonEvent { get; set; }
        public bool IsButtonSosEvent { get; set; }
    }

    public class TTNTrackerNearbyDevice
    {
        public byte[] MAC { get; set; }
        public byte RSSI { get; set; }

        public override string ToString()
        {
            return BitConverter.ToString(MAC, 0, MAC.Length) + " (RSSI=" + RSSI + ")";
        }
    }


    /// <summary>
    /// Definition of 1st byte of Data Frame from TTN
    /// </summary>
    public enum DataId : byte
    {
        /// <summary>
        /// Enum Default Value - not specified by seeed
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The Device Status package is uploaded when join LoRaWAN network. The Device Status packet 
        /// has two packet formats in different working modes:
        /// 1) Event Mode, ID = 0x01
        /// 2) Periodic Mode, ID = 0x02
        /// </summary>
        DeviceStatus_EventMode = 0x01,
        /// <summary>
        /// The Device Status package is uploaded when join LoRaWAN network. The Device Status packet 
        /// has two packet formats in different working modes:
        /// 1) Event Mode, ID = 0x01
        /// 2) Periodic Mode, ID = 0x02
        /// </summary>
        DeviceStatus_PeriodicMode = 0x02,
        /// <summary>
        /// hen no data is uploaded by the device within the heartbeat interval, a heartbeat packet will be 
        /// triggered. This packet only contains battery information.
        /// </summary>
        Heatbeat = 0x05,
        /// <summary>
        /// ID 0x06 is used to upload GNSS location, sensor data and battery
        /// </summary>
        Location_GNSS_Sensor_Battery = 0x06,
        /// <summary>
        /// ID 0x07 is used to upload Wi-Fi Mac addresses, sensor data and battery.
        /// </summary>
        Location_Wifi_Sensor = 0x07,
        /// <summary>
        /// ID 0x08 is used to upload Bluetooth Beacon MAC addresses, sensor data and battery.
        /// </summary>
        Location_Bluetooth_Sensor = 0x08,
        /// <summary>
        ///  GNSS Location Only Packet-0x09
        /// </summary>
        Location_GNSS_Only = 0x09,
        /// <summary>
        /// Wi-Fi Location Only Packet-0x0A
        /// </summary>
        Location_Wifi_Only = 0x0A,
        /// <summary>
        ///  Bluetooth Location Only Packet-0x0B
        /// </summary>
        Location_Bluetooth_Only = 0x0B,
        /// <summary>
        /// When the device cannot locate due to poor GNSS/ Wi-Fi/Bluetooth signal, the positioning timeout 
        /// packet is uploaded.
        /// </summary>
        Positioning_Timeout_and_ErrorCode = 0x0D,
    }
}
