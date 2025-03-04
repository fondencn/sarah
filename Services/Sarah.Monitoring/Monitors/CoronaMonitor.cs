using InteLuk.API.BusinessObjects;
using InteLuk.API.Interfaces;
using InteLuk.Data;
using InteLuk.Logging;
using InteLuk.ZWave.Notifications;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Sarah.Monitoring.Monitors
{
    public class CoronaMonitor : IDeseaseStatsProvider, ICanSelfTest
    {
        public string DeseaseName => "Corona";

        private const string RKI_API_URI = "https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_Landkreisdaten/FeatureServer/0/query?where=1%3D1&outFields=death_rate,cases,deaths,cases_per_100k,cases_per_population,county,last_update,cases7_per_100k,recovered,cases7_bl_per_100k&returnGeometry=false&returnDistinctValues=true&outSR=4326&f=json";

        private static readonly TimeSpan _UpdateInterval = TimeSpan.FromDays(1);
        private static readonly TimeSpan _UpdateIntervalImpftermine = TimeSpan.FromHours(3);

        private DateTime _lastUpdate;
        private bool _isImpftermine_Ludwigsburg_Available = false;

        private string RegionName { get; set; }


        private CoronaInfos CurrentCoronaInfos { get; set; }
        private CoronaInfos YesterdaysCoronaInfos { get; set; }

        private Task UpdateTask { get; set; }
        private Task UpdateImpftermineTask { get; set; }
        private CancellationTokenSource UpdateCancellationTokenSource { get; set; }

        public string Impftermine_Ludwigsburg { get; private set; }
        public bool IsImpftermine_Ludwigsburg_Available
        {
            get => _isImpftermine_Ludwigsburg_Available;
            private set
            {
                if (_isImpftermine_Ludwigsburg_Available != value)
                {
                    _isImpftermine_Ludwigsburg_Available = value;
                    NetworkEventAggregator.Instance.Report(new ImpftermineChangedEvent(value));
                }
            }
        }


        public string GetLocalStatsText()
        {
            if (this.CurrentCoronaInfos != null)
            {
                Feature regionStats = this.CurrentCoronaInfos.features.FirstOrDefault(item => item.attributes.county.Contains(this.RegionName, StringComparison.OrdinalIgnoreCase));
                Feature regionStatsYesterday = this.YesterdaysCoronaInfos?.features.FirstOrDefault(item => item.attributes.county.Contains(this.RegionName, StringComparison.OrdinalIgnoreCase));
                if (regionStats != null && this.CurrentCoronaInfos.fields?.Any() == true)
                {
                    //string resultText = DeseaseName + " Werte für " + regionStats.attributes.county + ": " +
                    //    "Durchschnittliche Fallzahlen der letzten 7 Tage: " + Math.Round(regionStats.attributes.cases7_per_100k, 0) + ", \r\n" +
                    //    this.CurrentCoronaInfos.fields.First(f => f.name == "cases").alias + ": " + regionStats.attributes.cases + ",   \r\n" +
                    //    this.CurrentCoronaInfos.fields.First(f => f.name == "deaths").alias + ": " + regionStats.attributes.deaths + ",   \r\n" +
                    //    //this.CurrentCoronaInfos.fields.First(f => f.name == "recovered").alias + ": " + (regionStats.attributes.recovered?.ToString(CultureInfo.CurrentCulture) ?? "Unbekannt") + ", " +
                    //    this.CurrentCoronaInfos.fields.First(f => f.name == "last_update").alias + ": " + regionStats.attributes.last_update + "  \r\n";
                    string heading = this.DeseaseName + " Werte für: " + regionStats.attributes.county + " ";
                    string sevenDaysAverage = "Durchschnittliche Fallzahlen der letzten 7 Tage: " + Math.Round(regionStats.attributes.cases7_per_100k, 0);
                    if (regionStatsYesterday?.attributes.cases7_per_100k != null)
                    {
                        double diff = regionStats.attributes.cases7_per_100k - regionStatsYesterday.attributes.cases7_per_100k;
                        diff = Math.Round(diff, 0);
                        if (diff != 0)
                        {
                            sevenDaysAverage += (" (" + (diff > 0 ? " plus " : "  ") + diff + ") ");
                        }
                        else
                        {
                            sevenDaysAverage += (" (kein Unterschied zu gestern) ");
                        }
                    }
                    string cases = "Fälle: " + regionStats.attributes.cases;
                    if (regionStatsYesterday?.attributes.cases != null)
                    {
                        double diff = regionStats.attributes.cases - regionStatsYesterday.attributes.cases;
                        if (diff != 0)
                        {
                            cases += (" (" + (diff > 0 ? " plus " : "") + diff + ") ");
                        }
                        else
                        {
                            cases += (" (kein Unterschied zu gestern) ");
                        }
                    }
                    string deaths = "Gestorben: " + regionStats.attributes.deaths;
                    if (regionStatsYesterday?.attributes.deaths != null)
                    {
                        double diff = regionStats.attributes.deaths - regionStatsYesterday.attributes.deaths;
                        if (diff != 0)
                        {
                            deaths += (" (" + (diff > 0 ? " plus " : " minus ") + diff + ") ");
                        }
                        else
                        {
                            deaths += (" (kein Unterschied zu gestern) ");
                        }
                    }
                    //string recovered = "Geheilt: " + (regionStats.attributes.recovered?.ToString(CultureInfo.CurrentCulture) ?? "Unbekannt");
                    string last_update = "Zuletzt aktualisiert: " + regionStats.attributes.last_update;


                    return string.Join(", " + Environment.NewLine, new string[] { heading, sevenDaysAverage, cases, deaths, last_update });
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }


        private ApplicationDbContext DB { get; }


        #region Singleton
        public static CoronaMonitor Instance { get; } = new CoronaMonitor();
        private CoronaMonitor()
        {
            this.DB = ApplicationDbContext.CreateDefault();
        }
        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~CoronaMonitor()
        {
            if (this.UpdateTask != null && this.UpdateTask.Status == TaskStatus.Running)
            {
                this.UpdateCancellationTokenSource.Cancel();
            }
        }
        #endregion

        /// <summary>
        /// Startet die Überwachung in einem eigenen Task
        /// </summary>
        /// <returns></returns>
        public Task Start(string regionName)
        {
            if (this.UpdateTask != null)
            {
                throw new InvalidOperationException("CoronaMonitor wurde bereits gestartet und kann nicht noch einmal gestartet werden.");
            }

            this.RegionName = regionName;
            CancellationTokenSource cts = new CancellationTokenSource();
            this.UpdateCancellationTokenSource = cts;
            this.UpdateTask = Task.Run(Update, cts.Token);
            
            // Impftermine im KIZ LB
            // --> Zur Zeit deaktiviert
            //this.UpdateImpftermineTask = Task.Run(UpdateImpftermine, cts.Token);

            /* Als Krankheitendienst registrieren */
            NotificationEngine.Instance.Deseases = this;

            Logger.Instance.LogDebug("CoronaMonitor gestartet und als Provider registriert.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Pollt das Kreisimpfzentrum Ludwigsburg, ob es neue Impftermine (momentan nur Kinder) gibt
        /// </summary>
        private async void UpdateImpftermine()
        {
            /* Sofort updaten, dann 1 h warten */
            await UpdateImpftermineFromKizLb();

            while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(_UpdateIntervalImpftermine);
                if (this.UpdateCancellationTokenSource.Token.IsCancellationRequested) break;


                await UpdateImpftermineFromKizLb();
            }
        }

        private async Task UpdateImpftermineFromKizLb()
        {
            try
            {
                string kizLbUrl = "https://kinderimpfung.kizlb.de/online/availableAppointments?data={%22idstation%22:%221%22,%22impfstoff%22:%22biontech%22}";

                var baseAddress = new Uri("https://kinderimpfung.kizlb.de");
                var cookieContainer = new CookieContainer();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                {

                    HttpClient client = new HttpClient(handler);
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(kizLbUrl),
                    };

                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    //request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    //request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    //request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("de-DE"));

                    //request.Headers.Add("content-type", "application/x-www-form-urlencoded");
                    //request.Headers.Add("cookie", "connect.sid=s%3A-0NLG-jlXGnkDHausdcnD06XMUeu7rlh.lJ5Jnj5TjbNBenf8%2FwAhjij5LsMgKbqm9y8E4%2Bfnrek; cookieconsent_status=dismiss");

                    cookieContainer.Add(baseAddress, new Cookie("connect.sid", "s%3A-0NLG-jlXGnkDHausdcnD06XMUeu7rlh.lJ5Jnj5TjbNBenf8%2FwAhjij5LsMgKbqm9y8E4%2Bfnrek"));
                    cookieContainer.Add(baseAddress, new Cookie("cookieconsent_status", "dismiss"));

                    request.Headers.Add("csrf-token", "fSL7Tpx4-casd6LjmXtXYZgYPK6O0_0s4QRw");
                    request.Headers.Add("referer", "https://kinderimpfung.kizlb.de/online/newAppointment");
                    request.Headers.Add("sec-ch-ua", " Not;A Brand\";v=\"99\",\"Microsoft Edge\";v=\"97\",\"Chromium\";v=\"97\"");
                    request.Headers.Add("sec-ch-ua-platform", "Windows");
                    request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36 Edg/97.0.1072.55");
                    request.Headers.Add("x-requested-with", "XMLHttpRequest");
                    //            accept: */*
                    //accept-encoding: gzip, deflate, br
                    //accept-language: de,de-DE;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6
                    //content-type: application/x-www-form-urlencoded
                    //cookie: connect.sid=s%3A-0NLG-jlXGnkDHausdcnD06XMUeu7rlh.lJ5Jnj5TjbNBenf8%2FwAhjij5LsMgKbqm9y8E4%2Bfnrek; cookieconsent_status=dismiss
                    //csrf-token: fSL7Tpx4-casd6LjmXtXYZgYPK6O0_0s4QRw
                    //referer: https://kinderimpfung.kizlb.de/online/newAppointment
                    //sec-ch-ua: " Not;A Brand";v="99", "Microsoft Edge";v="97", "Chromium";v="97"
                    //sec-ch-ua-mobile: ?0
                    //sec-ch-ua-platform: "Windows"
                    //sec-fetch-dest: empty
                    //sec-fetch-mode: cors
                    //sec-fetch-site: same-origin
                    //user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36 Edg/97.0.1072.55
                    //x-requested-with: XMLHttpRequest
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        string json = await response.Content.ReadAsStringAsync();
                        this.Impftermine_Ludwigsburg = json;

                        ImpfterminResponse deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<ImpfterminResponse>(json);
                        if (deserialized.TYPE?.Equals("NO_APP") == true)
                        {
                            /* Keine Termine verfügbar */
                            this.IsImpftermine_Ludwigsburg_Available = false;
                            Logger.Instance.LogInfo("Impftermine KIZ LB aktualisiert: Keine Termine verfügbar");

                        }
                        else
                        {
                            /* Termine verfügbar */
                            this.IsImpftermine_Ludwigsburg_Available = true;

                            Logger.Instance.LogInfo("Impftermine KIZ LB aktualisiert: Es sind Termine verfügbar");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Pollen der Impftermine", ex);
                this.Impftermine_Ludwigsburg = ex.Message;
            }
        }



        /// <summary>
        /// Regelmäßig Updateroutine dieser Klasse
        /// </summary>
        private async void Update()
        {
            /* Sofort updaten, dann 1 Tag warten */
            await UpdateCurrentCoronaStats();
            await UpdateCoronaHistory();
            await UpdateYesterdaysCoronaStats();
            this._lastUpdate = DateTime.Now;

            while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(_UpdateInterval);
                if (this.UpdateCancellationTokenSource.Token.IsCancellationRequested) break;


                await UpdateCurrentCoronaStats();
                await UpdateCoronaHistory();
                await UpdateYesterdaysCoronaStats();

                this._lastUpdate = DateTime.Now;
            }
        }

        private async Task UpdateYesterdaysCoronaStats()
        {
            try
            {
                var entries = await this.DB.DeseaseStats.ToListAsync();
                DeseaseKpi[] kpis = entries
                    .Where(item => item.DeseaseName == this.DeseaseName && item.Date.Date == DateTime.Today.AddDays(-1)).ToArray();
                this.YesterdaysCoronaInfos = CreateCoronaInfo(kpis);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Laden der Coronadaten von gstern", ex);
            }
        }


        private async Task UpdateCoronaHistory()
        {
            try
            {
                if (this.CurrentCoronaInfos != null
                    && !this.DB.DeseaseStats.Any(item => item.DeseaseName == this.DeseaseName && item.Date.Date == DateTime.Today))
                {
                    DeseaseKpi[] kpis = CreateHistoryEntries(this.CurrentCoronaInfos);
                    if (kpis != null)
                    {
                        await this.DB.DeseaseStats.AddRangeAsync(kpis);
                        await this.DB.SaveChangesAsync();
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Speichern der Coronadaten in der Verlaufsdatenbank", ex);
            }
        }

        private CoronaInfos CreateCoronaInfo(DeseaseKpi[] kpis)
        {
            if (kpis?.Any() != true)
            {
                return null;
            }
            else
            {
                CoronaInfos infos = new CoronaInfos();
                infos.features = new List<Feature>();
                Feature f = new Feature();
                f.attributes = new Attributes() { county = this.RegionName };
                infos.fields = new List<Field>(); /* Feldnamen werden nciht serialisiert */
                f.attributes.cases = ((int?)kpis.FirstOrDefault(item => item.FieldName == "cases")?.FieldValue).GetValueOrDefault(0);
                f.attributes.cases7_per_100k = ((int?)kpis.FirstOrDefault(item => item.FieldName == "cases7_per_100k")?.FieldValue).GetValueOrDefault(0);
                f.attributes.cases7_per_100k = ((int?)kpis.FirstOrDefault(item => item.FieldName == "cases7_per_100k")?.FieldValue).GetValueOrDefault(0);
                f.attributes.deaths = ((int?)kpis.FirstOrDefault(item => item.FieldName == "deaths")?.FieldValue).GetValueOrDefault(0);
                f.attributes.recovered = ((int?)kpis.FirstOrDefault(item => item.FieldName == "recovered")?.FieldValue).GetValueOrDefault(0);
                f.attributes.cases7_bl_per_100k = ((int?)kpis.FirstOrDefault(item => item.FieldName == "cases7_bl_per_100k")?.FieldValue).GetValueOrDefault(0);
                f.attributes.cases_per_100k = ((int?)kpis.FirstOrDefault(item => item.FieldName == "cases_per_100k")?.FieldValue).GetValueOrDefault(0);
                f.attributes.death_rate = ((int?)kpis.FirstOrDefault(item => item.FieldName == "death_rate")?.FieldValue).GetValueOrDefault(0);
                f.attributes.cases_per_population = ((int?)kpis.FirstOrDefault(item => item.FieldName == "cases_per_population")?.FieldValue).GetValueOrDefault(0);
                infos.features.Add(f);
                return infos;
            }
        }
        private DeseaseKpi[] CreateHistoryEntries(CoronaInfos currentCoronaInfos)
        {
            if (currentCoronaInfos == null)
            {
                return null;
            }
            else
            {
                Feature regionStats = this.CurrentCoronaInfos.features?.FirstOrDefault(item => item.attributes.county.Contains(this.RegionName, StringComparison.OrdinalIgnoreCase));
                if (regionStats != null && this.CurrentCoronaInfos.fields?.Any() == true)
                {
                    /* 15.12.2020, 00:00 Uhr */
                    string datePart = regionStats.attributes.last_update.Substring(0, regionStats.attributes.last_update.IndexOf(','));
                    DateTime stand = DateTime.Parse(datePart, CultureInfo.CurrentCulture);
                    List<DeseaseKpi> lst = new List<DeseaseKpi>();
                    lst.Add(new DeseaseKpi()
                    {
                        DeseaseName = this.DeseaseName,
                        Date = stand,
                        FieldName = "cases7_per_100k",
                        FieldValue = regionStats.attributes.cases7_per_100k
                    });
                    lst.Add(new DeseaseKpi()
                    {
                        DeseaseName = this.DeseaseName,
                        Date = stand,
                        FieldName = "cases",
                        FieldValue = regionStats.attributes.cases
                    });
                    lst.Add(new DeseaseKpi()
                    {
                        DeseaseName = this.DeseaseName,
                        Date = stand,
                        FieldName = "deaths",
                        FieldValue = regionStats.attributes.deaths
                    });
                    lst.Add(new DeseaseKpi()
                    {
                        DeseaseName = this.DeseaseName,
                        Date = stand,
                        FieldName = "recovered",
                        FieldValue = regionStats.attributes.recovered.GetValueOrDefault()
                    });

                    return lst.ToArray();
                }
                else
                {
                    return null;
                }
            }
        }

        private async Task UpdateCurrentCoronaStats()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(RKI_API_URI),
                };
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();

                    CoronaInfos currentWeather = Newtonsoft.Json.JsonConvert.DeserializeObject<CoronaInfos>(json);
                    this.CurrentCoronaInfos = currentWeather;
                    Logger.Instance.LogInfo("Coronadaten aktualisiert.");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Aktualisieren der Coronadaten", ex);
            }
        }

        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (this.CurrentCoronaInfos == null)
            {
                yield return new SelfTestResult(true, "Corona-Überwachung", "Keine Corona Infos geladen");
            }
            if ((DateTime.Now - this._lastUpdate) > TimeSpan.FromDays(2))
            {
                yield return new SelfTestResult(true, "Corona-Überwachung", "Die Daten sind älter als 2 Tage");
            }
        }





        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        private class UniqueIdField
        {
            public string name { get; set; }
            public bool isSystemMaintained { get; set; }
        }

        private class SpatialReference
        {
            public int wkid { get; set; }
            public int latestWkid { get; set; }
        }

        private class Field
        {
            public string name { get; set; }
            public string type { get; set; }
            public string alias { get; set; }
            public string sqlType { get; set; }
            public object domain { get; set; }
            public object defaultValue { get; set; }
            public int? length { get; set; }
        }

        private class Attributes
        {
            public double death_rate { get; set; }
            public int cases { get; set; }
            public int deaths { get; set; }
            public double cases_per_100k { get; set; }
            public double cases_per_population { get; set; }
            public string county { get; set; }
            public string last_update { get; set; }
            public double cases7_per_100k { get; set; }
            public int? recovered { get; set; }
            public double cases7_bl_per_100k { get; set; }
        }

        private class Feature
        {
            public Attributes attributes { get; set; }
        }

        private class CoronaInfos
        {
            public string objectIdFieldName { get; set; }
            public UniqueIdField uniqueIdField { get; set; }
            public string globalIdFieldName { get; set; }
            public string geometryType { get; set; }
            public SpatialReference spatialReference { get; set; }
            public List<Field> fields { get; set; }
            public List<Feature> features { get; set; }
        }


        private class ImpfterminResponse
        {
            public string TYPE { get; set; }
            //public string URL { get; set; }
            //public string MESSAGE { get; set; }
        }

    }
}
