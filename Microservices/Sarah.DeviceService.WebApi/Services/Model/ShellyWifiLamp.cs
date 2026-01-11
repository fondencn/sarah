using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWave.CommandClasses;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Sarah.DeviceService.Model
{
    public class ShellyWifiLamp : NetworkElement, ILamp
    {
        private byte _brightness;
        private string _color;
        private SensorData _meter;
        private Task _updateSensorDataTask;
        private CancellationTokenSource _UpdateSensorDataCancellationTokenSource;


        public string Hostname { get; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid"></param>
        public ShellyWifiLamp(byte nodeid, string ipOrHostname, IEventProcessingService events) : base(nodeid, events)
        {
            this.Hostname = ipOrHostname;
        }


        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~ShellyWifiLamp()
        {
            if (this._updateSensorDataTask != null && this._updateSensorDataTask.Status == TaskStatus.Running)
            {
                this._UpdateSensorDataCancellationTokenSource.Cancel();
            }
        }
        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            _logger?.LogInformation("Wifi Lamp " +
                 this.Hostname + ": start polling status...");
            CancellationTokenSource cts = new CancellationTokenSource();
            this._UpdateSensorDataCancellationTokenSource = cts;
            this._updateSensorDataTask = Task.Run(async () =>
            {

                while (!this._UpdateSensorDataCancellationTokenSource.Token.IsCancellationRequested)
                {
                    /* Wifi Lampen kann man immer Abfragen, die Haben ja keinen Akku */
                    await this.UpdateSensorData();
                    await Task.Delay(1 * 60 * 1000); // Check jede Minute
                }
            }, cts.Token);

            return Task.CompletedTask;
        }


        private async Task UpdateSensorData()
        {
            /* {"StatusSNS":{"Time":"2021-09-15T10:59:14","ENERGY":{"TotalStartTime":"2021-09-01T10:21:19","Total":7.901,"Yesterday":1.759,"Today":0.341,"Power":752,"ApparentPower":801,"ReactivePower":278,"Factor":0.94,"Voltage":229,"Current":3.500}}} */
            try
            {
                HttpClient client = new HttpClient();
                /* Stromverbraucht usw. Abfragen */
                string uri =  $"http://{this.Hostname}/status";
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    string responseJson = await response.Content.ReadAsStringAsync();
                    Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(responseJson);

                    this.Brightness = myDeserializedClass.lights[0].brightness;

                    string mode = myDeserializedClass.lights[0].mode;
                    if(mode.Equals("color"))
                    {
                        byte a, r, g, b;
                        a = 255;
                        r = myDeserializedClass.lights[0].red;
                        g = myDeserializedClass.lights[0].green;
                        b = myDeserializedClass.lights[0].blue;
                        System.Drawing.Color color = System.Drawing.Color.FromArgb(a, r, g, b);
                        this.Color = ColorConverter.ToHex(color);
                    } 
                    else
                    {
                        /* Weiß */
                        this.Color = "#FFFFFF";
                    }

                    float watts = myDeserializedClass.meters.Average(item => item.power);
                    this.Meter = new SensorData(watts, "W");
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("No route to host"))
                {
                    _logger?.LogError("Fehler beim Status Abfrage der Wifi Lampe " + this.Hostname, ex);
                }
            }
        }

        /// <summary>
        /// Helligkeit (aktuell)
        /// </summary>
        public byte Brightness
        {
            get => _brightness;
            private set { if (_brightness != value) { _brightness = value; ReportEvent(new NetworkEvent<byte>(this.NodeID, value)); } }
        }

        /// <summary>
        /// Farbe (aktuell)
        /// </summary>
        public string Color
        {
            get => _color;
            private set { if (_color != value) { _color = value; if (value != "?") { ReportEvent(new NetworkEvent<string>(this.NodeID, value)); } } }
        }


        /// <summary>
        /// Meter
        /// </summary>
        public SensorData Meter { get => _meter; private set { if (_meter != value) { _meter = value; ReportEvent(new NetworkEvent<string>(this.NodeID, value?.ToString())); } } }

        /// <summary>
        /// Letzte Änderungszeitpunkt
        /// </summary>
        public DateTime? LastChange { get; private set; }
        public override bool? IsActive => this.Brightness > 0;
        public LampColorModes ColorMode => LampColorModes.RGBWW;

        public Animation CurrentAnimation { get; set; }


        public override string ClassDescription => "Lampe";

        public override string StateInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Helligkeit: " + this.Brightness + (IsActive == true ? "💡" : "🌙") + "<br/>");
                sb.Append("Farbe: " + this.Color + "<br/>");
                sb.Append("Leistung: " + this.Meter + "<br/>");
                return sb.ToString();
            }
        }

        public Task SetBrightness(byte brightness)
        {
            HttpClient http = new HttpClient();
            string url = $"http://{this.Hostname}/light/0?brightness={Math.Min((byte)100, brightness)}";
            return http.GetAsync(url);
        }

        public Task SetColdWhite()
        {
            HttpClient http = new HttpClient();
            string url = $"http://{this.Hostname}/light/0?mode=white&temp=6465";
            return http.GetAsync(url);
        }
        public Task SetWarmWhite()
        {
            HttpClient http = new HttpClient();
            string url = $"http://{this.Hostname}/light/0?mode=white&temp=4050";
            return http.GetAsync(url);
        }

        public Task SetColor(string color)
        {
            byte a, red, green, blue;
            ColorConverter.FromHex(color, out a, out red, out green, out blue);

            HttpClient http = new HttpClient();
            string url = $"http://{this.Hostname}/light/0?mode=color&red={red}&green={green}&blue={blue}";
            return http.GetAsync(url);
        }


        public Task ToggleState() => this.SetBrightness(this.Brightness > 0 ? (byte)0 : (byte)100);




        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class ActionsStats
        {
            public int skipped { get; set; }
        }

        public class Cloud
        {
            public bool enabled { get; set; }
            public bool connected { get; set; }
        }

        public class Light
        {
            public bool ison { get; set; }
            public string source { get; set; }
            public bool has_timer { get; set; }
            public int timer_started { get; set; }
            public int timer_duration { get; set; }
            public int timer_remaining { get; set; }
            public string mode { get; set; }
            public byte red { get; set; }
            public byte green { get; set; }
            public byte blue { get; set; }
            public int white { get; set; }
            public int gain { get; set; }
            public int temp { get; set; }
            public byte brightness { get; set; }
            public int effect { get; set; }
            public int transition { get; set; }
        }

        public class PowerMeter
        {
            public float power { get; set; }
            public bool is_valid { get; set; }
            public int timestamp { get; set; }
            public List<double> counters { get; set; }
            public int total { get; set; }
        }

        public class Mqtt
        {
            public bool connected { get; set; }
        }

        public class Root
        {
            public WifiSta wifi_sta { get; set; }
            public Cloud cloud { get; set; }
            public Mqtt mqtt { get; set; }
            public string time { get; set; }
            public int unixtime { get; set; }
            public int serial { get; set; }
            public bool has_update { get; set; }
            public string mac { get; set; }
            public int cfg_changed_cnt { get; set; }
            public ActionsStats actions_stats { get; set; }
            public List<Light> lights { get; set; }
            public List<PowerMeter> meters { get; set; }
            public Update update { get; set; }
            public int ram_total { get; set; }
            public int ram_free { get; set; }
            public int fs_size { get; set; }
            public int fs_free { get; set; }
            public int uptime { get; set; }
        }

        public class Update
        {
            public string status { get; set; }
            public bool has_update { get; set; }
            public string new_version { get; set; }
            public string old_version { get; set; }
        }

        public class WifiSta
        {
            public bool connected { get; set; }
            public string ssid { get; set; }
            public string ip { get; set; }
            public int rssi { get; set; }
        }


    }



}
