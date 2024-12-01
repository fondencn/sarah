using Sarah.API.Business;
using Sarah.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sarah.API.Interfaces.Services;

namespace Sarah.DeviceService.Model
{
    /// <summary>
    /// WLAN / Wifi Steckdose
    /// </summary>
    public class WifiWallPlug : WallPlug
    {
        private static readonly string _PowerOnOffUriTemplate = "http://{0}/cm?cmnd=power%20{1}";
        private static readonly string _StatusUriTemplate = "http://{0}/cm?cmnd=status{1}";
        private Task _updateSensorDataTask;
        private CancellationTokenSource _UpdateSensorDataCancellationTokenSource;


        public string Hostname { get; }

        public WifiWallPlug(byte nodeid, string hostname) : base(nodeid)
        {
            this.Hostname = hostname;
        }



        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~WifiWallPlug()
        {
            if (this._updateSensorDataTask != null && this._updateSensorDataTask.Status == TaskStatus.Running)
            {
                this._UpdateSensorDataCancellationTokenSource.Cancel();
            }
        }

        public override Task InitializeAsync(IDeviceService deviceService)
        {
            Logger.Instance.LogInfo("Wifi WallPlug " +
                 this.Hostname + ": start polling status...");
                
            CancellationTokenSource cts = new CancellationTokenSource();
            this._UpdateSensorDataCancellationTokenSource = cts;
            this._updateSensorDataTask = Task.Run(async () =>
            {

                while (!this._UpdateSensorDataCancellationTokenSource.Token.IsCancellationRequested)
                {
                    /* Wifi Dosen kann man immer Abfragen, die Haben ja keinen Akku */
                    await this.UpdateSensorData();
                    await Task.Delay(1 * 60 * 1000); // Check jede Minute
                }
            }, cts.Token);

            return Task.CompletedTask;
        }

        public override async Task SetState(bool newState)
        {
            string uri = String.Format(_PowerOnOffUriTemplate, Hostname, newState ? "1" : "0");
            try
            {
                HttpClient client = new HttpClient();
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    string responseJson = await response.Content.ReadAsStringAsync();
                    PowerResponse newStateResponse = JsonConvert.DeserializeObject<PowerResponse>(responseJson);
                    this.IsOn = String.Equals(newStateResponse.POWER, "ON", StringComparison.OrdinalIgnoreCase);

                    /* Anliegende Leistung abfragen */
                    await UpdateSensorData();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Setzen des Zustandes der Wifi Steckdose " + this.Hostname + " auf " + newState + ", Uri war " + uri, ex);
            }
        }

        private async Task UpdateSensorData()
        {
            /* {"StatusSNS":{"Time":"2021-09-15T10:59:14","ENERGY":{"TotalStartTime":"2021-09-01T10:21:19","Total":7.901,"Yesterday":1.759,"Today":0.341,"Power":752,"ApparentPower":801,"ReactivePower":278,"Factor":0.94,"Voltage":229,"Current":3.500}}} */
            try
            {
                HttpClient client = new HttpClient();
                /* Stromverbraucht usw. Abfragen */
                string uri = String.Format(_StatusUriTemplate, Hostname, "%2010");
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    string responseJson = await response.Content.ReadAsStringAsync();
                    StatusResponse statusResponse = JsonConvert.DeserializeObject<StatusResponse>(responseJson);
                    Energy e = statusResponse.StatusSNS.ENERGY;

                    if (e != null)
                    {
                        this.Meter_W = new SensorData(e.Power, "W");
                        this.Meter_A = new SensorData((float)e.Current, "A");
                        this.Meter_kVAh = new SensorData(e.ApparentPower, "VA");
                        this.Meter_kWh = new SensorData((float)e.Total, "kWh");
                    }
                }

                /* Allgemeinen On(Off Zustand abfragen */
                uri = String.Format(_StatusUriTemplate, Hostname, "%2011");
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    string responseJson = await response.Content.ReadAsStringAsync();
                    PowerResponse2 statusResponseGeneral = JsonConvert.DeserializeObject<PowerResponse2>(responseJson);
                    this.IsOn = String.Equals(statusResponseGeneral.StatusSTS.POWER, "ON", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("No route to host"))
                {
                    Logger.Instance.LogException("Fehler beim Status Abfrage der Wifi Steckdose " + this.Hostname, ex);
                }
            }
        }

        private class PowerResponse
        {
            public string POWER { get; set; }
        }

        private class PowerResponse2
        {
            public StatusSTS StatusSTS { get; set; }
        }

        public class StatusResponse
        {
            public StatusSNS StatusSNS { get; set; }
        }

        public partial class StatusSNS
        {
            public DateTimeOffset Time { get; set; }

            public Energy ENERGY { get; set; }
        }

        public partial class Energy
        {
            public DateTimeOffset TotalStartTime { get; set; }

            public double Total { get; set; }

            public double Yesterday { get; set; }

            public double Today { get; set; }

            public long Power { get; set; }

            public long ApparentPower { get; set; }

            public long ReactivePower { get; set; }

            public double Factor { get; set; }

            public long Voltage { get; set; }

            public double Current { get; set; }
        }


        public partial class StatusSTS
        {
            public DateTimeOffset Time { get; set; }

            public string Uptime { get; set; }

            public double Vcc { get; set; }

            public string SleepMode { get; set; }

            public long Sleep { get; set; }

            public long LoadAvg { get; set; }

            public string POWER { get; set; }

            public Wifi Wifi { get; set; }
        }

        public partial class Wifi
        {
            public long AP { get; set; }

            public string SSId { get; set; }

            public string BSSId { get; set; }

            public long Channel { get; set; }

            public long RSSI { get; set; }

            public long LinkCount { get; set; }

            public string Downtime { get; set; }
        }
    }
}
