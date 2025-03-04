using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Logging;
using System.Text.RegularExpressions;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Überwachung für Wetterwarnungen (In-Memory, Datenquelle DWD-Warnwetter)
    /// </summary>
    public class WeatherMonitor : IWeatherProvider, ICanSelfTest
    {
        private static readonly Uri _DwdUri = new Uri("https://www.dwd.de/DWD/warnungen/warnapp/json/warnings.json");
        private static readonly TimeSpan _UpdateInterval = TimeSpan.FromMinutes(30);
#if DEBUG
        private static readonly TimeSpan _UpdateIntervalWarnings = TimeSpan.FromMinutes(1);
#else
        private static readonly TimeSpan _UpdateIntervalWarnings = TimeSpan.FromMinutes(10);
#endif
        private static readonly TimeSpan _WarnInterval = TimeSpan.FromHours(3);
        private string WarnLocation { get; set; }

        private Task UpdateTask { get; set; }
        private Task UpdateWarningsTask { get; set; }
        private CancellationTokenSource UpdateCancellationTokenSource { get; set; }

        /// <summary>
        /// DIe aktuellen Warnungen für den eingestellten Landkreis (DWD-Format)
        /// </summary>
        public List<DwdWarning> CurrentLocalWeatherWarnings { get; } = new List<DwdWarning>();

        /// <summary>
        /// Die aktuelle Wettervorhersage für den einstellten Ort
        /// </summary>
        public OpenWeather.CurrentWeather.WeatherForecast CurrentWeather { get; set; }

        /// <summary>
        /// Die Wettervorhersage
        /// </summary>
        public OpenWeather.Forecast.Root WeatherForecast { get; set; }

        private DateTime LastUpdate { get; set; }
        private DateTime LastUpdateWarnings { get; set; }


        /// <summary>
        /// OpenWeatherMap REST API freier Schlüssel für diese Anwendung
        /// </summary>
        private static string OpenWeatherMap_ApiKey { get; } = "84bbd04458c25bf51c231b0e884ebdfd";



        /// <summary>
        /// Die Aaktuelle außentemperatur oder die Standard-Raumtemperatur falls nichts bekannt ist.
        /// </summary>
        public double CurrentOutdoorTemperature => (CurrentWeather?.main?.temp).GetValueOrDefault(22);

        #region Singleton
        public static WeatherMonitor Instance { get; } = new WeatherMonitor();

        private WeatherMonitor()
        {

        }
        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~WeatherMonitor()
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
                throw new InvalidOperationException("WeatherMonitor wurde bereits gestartet und kann nicht noch einmal gestartet werden.");
            }
            this.WarnLocation = regionName;
            CancellationTokenSource cts = new CancellationTokenSource();
            this.UpdateCancellationTokenSource = cts;
            this.UpdateTask = Task.Run(Update, cts.Token);
            this.UpdateWarningsTask = Task.Run(UpdateWarnings, cts.Token);

            Logger.Instance.LogDebug("WeatherMonitor gestartet und als Provider registriert.");

            return Task.CompletedTask;
        }
        /// <summary>
        /// Regelmäßig Updateroutine dieser Klasse für die Wettervorhersage
        /// </summary>
        private async Task Update()
        {
            await UpdateCurrentWeather();
            await UpdateForecast();
            this.LastUpdate = DateTime.Now;

            while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(_UpdateInterval);
                if (this.UpdateCancellationTokenSource.Token.IsCancellationRequested) break;

                await UpdateCurrentWeather();
                await UpdateForecast();
                this.LastUpdate = DateTime.Now;
            }
        }
        /// <summary>
        /// Regelmäßig Updateroutine dieser Klasse für die Wetterwarnungen (öfter)
        /// </summary>
        private async Task UpdateWarnings()
        {
            await UpdateWeatherWarnings();
            this.LastUpdateWarnings = DateTime.Now;

            while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(_UpdateIntervalWarnings);
                if (this.UpdateCancellationTokenSource.Token.IsCancellationRequested) break;

                await UpdateWeatherWarnings();
                this.LastUpdateWarnings = DateTime.Now;

            }
        }

        private async Task UpdateForecast()
        {

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://api.openweathermap.org/data/2.5/forecast?q=" + this.WarnLocation + "&appid=" + OpenWeatherMap_ApiKey + "&lang=de&units=metric")
                };
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();
                    OpenWeather.Forecast.Root forecast = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenWeather.Forecast.Root>(json);
                    this.WeatherForecast = forecast;
                    Logger.Instance.LogInfo("Wettervorhersage für " + forecast.city.name + " aktualisiert (" + forecast.cnt + " Elemente): " + forecast.message);
                    //NetworkEventAggregator.Instance.Report(new OutDoorTemperatureChangedEvent(currentWeather.main.temp));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Aktualisieren der Wettervorhersage", ex);
            }
        }

        private async Task UpdateCurrentWeather()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://api.openweathermap.org/data/2.5/weather?q=" + this.WarnLocation + "&appid=" + OpenWeatherMap_ApiKey + "&lang=de&units=metric")
                };
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();
                    OpenWeather.CurrentWeather.WeatherForecast currentWeather = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenWeather.CurrentWeather.WeatherForecast>(json);
                    this.CurrentWeather = currentWeather;
                    Logger.Instance.LogInfo("Aktuelles Wetter für " + currentWeather.name + " aktualisiert: " + currentWeather.DisplayText);
                    NetworkEventAggregator.Instance.Report(new OutDoorTemperatureChangedEvent(currentWeather.main.temp));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Aktualisieren der Wetterdaten", ex);
            }
        }

        /// <summary>
        /// Parser für DWD-Wetterwarnungen
        /// </summary>
        private async Task UpdateWeatherWarnings()
        {
            try
            {
                Logger.Instance.LogError(nameof(UpdateWeatherWarnings));

                HttpClient http = new HttpClient();
                string resultJson = await http.GetStringAsync(_DwdUri);
                resultJson = resultJson.Replace('\n', ' ').Replace('\r', ' ');
                resultJson = resultJson.Substring("warnWetter.loadWarnings(".Length, resultJson.Length - "warnWetter.loadWarnings(".Length - 2);
                DwdWarnings deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<DwdWarnings>(resultJson);


                IEnumerable<DwdWarning> ludwigsburgWarnings = deserialized.warnings
                    .Where(item => item.Value.Any(itemVal => itemVal.regionName.Contains(this.WarnLocation, StringComparison.OrdinalIgnoreCase)))
                    .SelectMany(item => item.Value)
                    .Concat(
                        deserialized.vorabInformation.Where(item => item.Value.Any(itemVal => itemVal.regionName.Contains(this.WarnLocation, StringComparison.OrdinalIgnoreCase)))
                        .SelectMany(item => item.Value)
                    )
                    .ToList();

                /* cleanup old */
                IEnumerable<DwdWarning> itemsToRemove = this.CurrentLocalWeatherWarnings
                    .Where(item => !ludwigsburgWarnings.Any(item => item.Key.Equals(item.Key, StringComparison.CurrentCultureIgnoreCase)))
                    .ToList();
                foreach (DwdWarning kitemToRemove in itemsToRemove)
                {
                    Logger.Instance.LogInfo("Entfernte Wetterwarnung: " + kitemToRemove.GetOutputString());
                    this.CurrentLocalWeatherWarnings.Remove(kitemToRemove);
                }

                /* add new */
                var entriesNew = ludwigsburgWarnings
                    .Where(item => !this.CurrentLocalWeatherWarnings.Any(item2 => item2.Key.Equals(item.Key, StringComparison.CurrentCultureIgnoreCase)))
                    .ToList();
                foreach (var entry in entriesNew)
                {
                    string str = "Neue Wetterwarnung:  ";
                    str += "Headline: " + entry.headline + Environment.NewLine;
                    str += "Description: " + entry.description + Environment.NewLine;
                    str += "Event: " + entry.@event + Environment.NewLine;
                    str += "Start: " + entry.StartDate + Environment.NewLine;
                    str += "End: " + entry.EndDate + Environment.NewLine;
                    str += "instruction: " + entry.instruction + Environment.NewLine;
                    str += "LastWarn: " + entry.LastWarn + Environment.NewLine;
                    Logger.Instance.LogError(str);
                    NetworkEventAggregator.Instance.Report(new WeatherWarningEvent(entry.@event));
                    CurrentLocalWeatherWarnings.Add(entry);
                }

                /* check pending warnings */
                RaisePendingWarnings();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Aktualisieren der Wetterwarnungen", ex);
            }
        }


        /// <summary>
        /// Löst für alle Warnungen die neu sind oder das Warnintervall überschritten haben, eine Sprachwarnung aus
        /// </summary>
        /// <returns></returns>
        private void RaisePendingWarnings()
        {
            List<string> warningMessages = new List<string>();
            foreach (DwdWarning warning in CurrentLocalWeatherWarnings)
            {
                bool warnNow = false;
                DateTime now = DateTime.Now;
                if (!warning.LastWarn.HasValue)
                {
                    warnNow = true;
                }
                else if ((now - warning.LastWarn.Value) > _WarnInterval)
                {
                    warnNow = true;
                }
                /* SilentHours beachten */
                warnNow &= !now.IsInSilentTime(_config);

                /* gültigkeitszeit beachten */
                if (warning.EndDate.HasValue && !warning.IsAllDayWarning)
                {
                    warnNow &= warning.EndDate > now;
                }

                if (warnNow)
                {
                    warning.LastWarn = now;
                    warningMessages.Add(warning.GetOutputString());
                }
            }

            if (warningMessages.Any())
            {
                string warnMessage = "Achtung, Wetterwarnung für " + this.CurrentLocalWeatherWarnings.First().regionName + ": "
                    + String.Join(". " + Environment.NewLine, warningMessages.Distinct());
                NotificationEngine.Instance.Voice.Say(warnMessage, NotificationEngine.BroadcastAllSpeakers);
                Logger.Instance.LogInfo(warnMessage);
            }
        }


        /// <summary>
        /// Ermittelt einen Text zur Wettervorhersage am angegeben Tag
        /// </summary>
        /// <param name="dteDate"></param>
        /// <returns></returns>
        public string GetWeatherForecastString(DateTime dteDate)
        {
            string result = "";

            if (this.WeatherForecast?.list?.Any(item => item.Date.Date == dteDate.Date) == true)
            {
                var weatherForDate = this.WeatherForecast.list.Where(item => item.Date.Date == dteDate.Date);
                result = "Wetter am " + dteDate.Date.ToString("dd.MM.") + ": ";
                var morgens = weatherForDate.Where(item => item.Date.Hour < 12);
                var mittags = weatherForDate.Where(item => item.Date.Hour >= 12 && item.Date.Hour < 17);
                var abends = weatherForDate.Where(item => item.Date.Hour >= 17 && item.Date.Hour <= 19);
                var nachts = weatherForDate.Where(item => item.Date.Hour >= 19 && item.Date.Hour <= 24);
                result += "Morgens: " + GetWeatherString(morgens) + ". ";
                result += ", Mittags: " + GetWeatherString(mittags) + ". ";
                result += ", Abends: " + GetWeatherString(abends) + ". ";
                result += ", Nachts: " + GetWeatherString(nachts) + ". ";
            }
            else
            {
                result = "Keine Wettervorhersage bekannt";
            }

            return result;
        }

        /// <summary>
        /// Ermittelt einen Text zur Wettervorhersage am angegeben Tag
        /// </summary>
        /// <param name="dteDate"></param>
        /// <returns></returns>
        public string GetWeatherForecastStringForToday()
        {
            string result = "";
            DateTime now = DateTime.Now;

            if (this.WeatherForecast?.list?.Any() == true)
            {
                result = "Wetter für ";
                if (now.Hour < 10)
                {
                    var weatherForDate = this.WeatherForecast.list.Where(item => item.Date.Date == now.Date);
                    var morgens = weatherForDate.Where(item => item.Date.Hour < 12);
                    var mittags = weatherForDate.Where(item => item.Date.Hour >= 12 && item.Date.Hour < 17);
                    result += "Heute morgen: " + GetWeatherString(morgens) + ". ";
                    result += ", heute Nachmittag: " + GetWeatherString(mittags) + ". ";
                }
                if (now.Hour >= 10 && now.Hour < 18)
                {
                    var weatherForDate = this.WeatherForecast.list.Where(item => item.Date.Date == now.Date);
                    var mittags = weatherForDate.Where(item => item.Date.Hour >= 12 && item.Date.Hour < 17);
                    result += "Heute Nachmittag: " + GetWeatherString(mittags) + ". ";
                }
                else if (now.Hour >= 18)
                {
                    var weatherForDate = this.WeatherForecast.list.Where(item => item.Date.Date == now.Date);
                    var weatherForTomorrow = this.WeatherForecast.list.Where(item => item.Date.Date == now.AddDays(1).Date);
                    var nachts = weatherForDate.Where(item => item.Date.Hour >= 19 && item.Date.Hour <= 24);
                    var morgens = weatherForTomorrow.Where(item => item.Date.Hour < 12);
                    result += "Heute Nacht: " + GetWeatherString(nachts) + ". ";
                    result += ", Morgen früh: " + GetWeatherString(morgens) + ". ";
                }
            }

            return result;
        }


        private string GetWeatherString(IEnumerable<OpenWeather.Forecast.List> weatherItems)
        {
            if (weatherItems?.Any() == true)
            {
                double rainmm = Math.Round(weatherItems.Average(item => (item.rain?._3h).GetValueOrDefault(0)), 0);
                double snowmm = Math.Round(weatherItems.Average(item => (item.snow?._3h).GetValueOrDefault(0)), 0);

                string res = weatherItems.First().weather.First().description
                    + " bei " + Math.Round(weatherItems.Average(item => item.main.temp), 0) + "°C";

                if (rainmm > 0)
                {
                    res += " , Regen: ungefähr" + rainmm + " mm";
                }
                if (snowmm > 0)
                {
                    res += " , Schneee: ungefähr" + rainmm + " mm";
                }

                return res;
            }
            else
            {
                return "Keine Wetterdaten";
            }
        }

        public string GetCurrentWeatherString(bool addDebugOutput = false, bool getWarningDetails = false)
        {
            string result = "";

            if (this.CurrentWeather != null)
            {
                result += this.CurrentWeather.DisplayText;
                result += "  " + Environment.NewLine;
            }


            if (this.CurrentLocalWeatherWarnings.Any())
            {
                string warnMessage = "Achtung, Wetterwarnung für " + this.CurrentLocalWeatherWarnings.First().regionName + ": " + Environment.NewLine;
                List<string> warningMsgs = new List<string>();
                foreach (DwdWarning warning in CurrentLocalWeatherWarnings)
                {
                    if (addDebugOutput)
                    {
                        result += "Headline: " + warning.headline + Environment.NewLine;
                        result += "Description: " + warning.description + Environment.NewLine;
                        result += "Event: " + warning.@event + Environment.NewLine;
                        result += "Start: " + warning.StartDate + Environment.NewLine;
                        result += "End: " + warning.EndDate + Environment.NewLine;
                        result += "type: " + warning.type + Environment.NewLine;
                        result += "instruction: " + warning.instruction + Environment.NewLine;
                        result += "LastWarn: " + warning.LastWarn + Environment.NewLine;
                    }
                    warningMsgs.Add(warning.GetOutputString(getWarningDetails));
                }
                result += String.Join(" " + Environment.NewLine + Environment.NewLine,
                    warningMsgs.Distinct()); //Keine Duplikate, nur weil die Zeit leicht anders ist
            }
            return result;
        }

        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (this.UpdateTask == null)
            {
                yield return new SelfTestResult(true, "Wetterüberwachung", "Die Wetterüberwachung ist nicht gestartet.");
            }
            if (this.UpdateWarningsTask == null)
            {
                yield return new SelfTestResult(true, "Wetterüberwachung", "Die Wetterwarnungsüberwachung ist nicht gestartet.");
            }
            if (this.CurrentWeather == null)
            {
                yield return new SelfTestResult(true, "Wetterüberwachung", "Kein Wetter für die aktuelle region geladen.");
            }
            if ((DateTime.Now - this.LastUpdate) > TimeSpan.FromHours(2))
            {
                yield return new SelfTestResult(true, "Wetterüberwachung", "Die aktuellen Wetterdaten sind älter als 2 Stunden.");
            }
            if ((DateTime.Now - this.LastUpdateWarnings) > TimeSpan.FromHours(2))
            {
                yield return new SelfTestResult(true, "Wetterüberwachung", "Die aktuellen Warnungsdaten sind älter als 2 Stunden.");
            }
        }

        public string GetWeatherWarningString() 
            => String.Join(". ", this.CurrentLocalWeatherWarnings.Select(w => w.GetOutputString()));


        public DateTime? GetSunrise()
        {
            if (this.CurrentWeather != null)
            {
                return UnixTime.GetDateTimeFromLinuxEpochSeconds(this.CurrentWeather.sys.sunrise);
            } 
            else
            {
                return null;
            }
        }





        #region weather warnings
        /// <summary>
        /// Daten des DWD
        /// </summary>
        internal class DwdWarnings
        {
            /// <summary>
            /// Zeitpunkt der letzten aktualisierung
            /// </summary>
            public long time { get; set; }

            /// <summary>
            /// Liste mit Warnungen (key=id)
            /// </summary>
            public Dictionary<string, DwdWarning[]> warnings { get; set; }

            /// <summary>
            /// Liste mit Vorabinformationen zu Wetterlagen
            /// </summary>
            public Dictionary<string, DwdWarning[]> vorabInformation { get; set; }

        }




        /// <summary>
        /// Einzelne Warnung des DWD
        /// </summary>
        public class DwdWarning : IEquatable<DwdWarning>, IComparable<DwdWarning>
        {

            /// <summary>
            /// Eindeutiger schlüssel berechnet aus dem Warntext, dem Start- und dem Enddatum (sofern verfügbar)
            /// </summary>
            public string Key => description + "|" + start.GetValueOrDefault(0) + "|" + end.GetValueOrDefault(0);

            public string regionName { get; set; }
            public long? end { get; set; }
            public long? start { get; set; }
            public int? type { get; set; }
            public string state { get; set; }
            public int? level { get; set; }
            public string description { get; set; }
            public string @event { get; set; }
            public string headline { get; set; }
            public string instruction { get; set; }
            public string stateShort { get; set; }
            /// <summary>
            /// Gibt an, wann dieses System zuletzt für diese Warnung eine Ausgabe gemacht hat
            /// </summary>
            public DateTime? LastWarn { get; set; }
            /// <summary>
            /// Start des Warnzeitraums für diese Warnung
            /// </summary>
            public DateTime? StartDate => UnixTime.GetDateTimeFromLinuxEpochMilliseconds(this.start);
            /// <summary>
            /// Ende des Warnzeitraums für diese Warnung
            /// </summary>
            public DateTime? EndDate => UnixTime.GetDateTimeFromLinuxEpochMilliseconds(this.end);

            /// <summary>
            /// Gibt an, ob die Warnung für den ganzen Tag gillt (laut DWD-API StartDate == EndDate)
            /// </summary>
            public bool IsAllDayWarning => StartDate == EndDate;

            public int CompareTo(DwdWarning other)
            {
                return String.Compare(this.Key, other?.Key);
            }

            public bool Equals(DwdWarning other)
            {
                return String.Equals(this.Key, other?.Key);
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }


            /// <summary>
            /// Baut einen Anzeigestring für diese Wetterwarnung zusammen
            /// </summary>
            /// <returns></returns>
            public string GetOutputString(bool getWarningDetails = false)
            {
                string str;
                str = getWarningDetails ? this.description : this.headline;
                if (this.StartDate.HasValue)
                {
                    if ((this.StartDate.Value - DateTime.Now).TotalHours <= 0)
                    {
                        str = getWarningDetails ? this.description : this.headline; //in der Vergangenheit - keine Uhrzeit dazu ausgeben
                    }
                    else if ((this.StartDate.Value - DateTime.Now).TotalHours <= 2)
                    {
                        str = getWarningDetails ? this.description : this.headline; //demnächst - auch keine Uhrzeit dazu ausgeben
                    }
                    else if (this.StartDate.Value.Date == DateTime.Today)
                    {
                        // Heute aber nicht innerhalb der nächsten 2 Stunden
                        str = "Heute ab " + this.StartDate.Value.ToString("HH:mm") + " Uhr: " + str;
                    }
                    else if (this.StartDate.Value.Date == DateTime.Today.AddDays(1))
                    {
                        // Morgen
                        str = "Morgen ab " + this.StartDate.Value.ToString("HH:mm") + " Uhr: " + str;
                    }
                    else if (this.StartDate.Value.Date == DateTime.Today.AddDays(2))
                    {
                        // Übermorgen
                        str = "Übermorgen ab " + this.StartDate.Value.ToString("HH:mm") + " Uhr: " + str;
                    }
                    else
                    {
                        // Irgendwann anders: Datum ausgeben
                        str = "Am " + this.start.Value.ToString("dd.MM HH:mm") + " Uhr: " + str;
                    }


                    Regex regexWind = new Regex("([0-9]+) km/h", RegexOptions.IgnoreCase);
                    MatchCollection windmatches = regexWind.Matches(this.description);
                    /* Bei Windwarnungen die Windgeschwindigkeit es der Description mit anhängen. Nicht alle Details, aber verkürzt nur die km/h */
                    if (windmatches.Any() && !getWarningDetails)
                    {
                        int maxKmh = windmatches.Select(m => Int32.Parse(m.Groups[1].Value)).Max();
                        str += ". Die Windgeschwindigkeit beträgt bis zu " + maxKmh + " Kilometer pro Stunde.";
                    }
                }

                /* Verhaltensregeln hinzufügen */
                //str += ", " + Environment.NewLine + this.instruction;

                // m/s, kn und bft raussschmeissen, uns interessieren nur die km/h
                //"Es treten oberhalb 600 m Sturmböen mit Geschwindigkeiten zwischen 70 km/h (20m/s, 38kn, Bft 8) und 85 km/h (24m/s, 47kn, Bft 9) aus südwestlicher Richtung auf.
                Regex regex = new Regex(@"\(([^)]*)\)");
                MatchCollection matches = regex.Matches(str);
                if (matches.Count > 0)
                {
                    foreach (Match m in matches)
                    {
                        str = str.Replace(m.Value, string.Empty);
                    }
                }

                return str;
            }
        }

        #endregion
    }
}
