using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Microsoft.Extensions.Logging;
using Ical.Net;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Stellt Daten für Schulferien für das System bereit
    /// </summary>
    public class FerienMonitor (IConfiguration _config, ILogger<FerienMonitor> _logger) : IFerienInfoProvider, ICanSelfTest, IMonitor
    {
        /// <summary>
        /// Die bekannten Schulferien als vereinheitlichte Liste
        /// </summary>
        public IReadOnlyCollection<Ferien> Ferien => FerienDateien.Instance.Items
            ?.SelectMany(ferienFile => ferienFile.Ferien)
            ?.ToList()
            ?.AsReadOnly() 
            ?? new List<Ferien>()
            .AsReadOnly();


        /// <summary>
        /// Gibt das aktuelle Ferienelemente (für heute) zurück oder NULL, falls keine Ferien sind.
        /// </summary>
        public Ferien? AktuelleFerien => this.Ferien?.FirstOrDefault(item => item.Start <= DateTime.Now && item.Ende >= DateTime.Now);

    

        /// <summary>
        /// Lädt alle bekannten Ferien aus den Dateien im iCal Unterordner
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            string iCalFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "/", _config["iCalFolder"] ?? "");
            FerienDateien.Instance.Load(iCalFolder);
            _logger.LogDebug("{FerienElementCount} Ferienelemente geladen.", FerienDateien.Instance.Items?.Count ?? 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Die Selbsttestfunktion
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (this.Ferien?.Any() != true)
            {
                yield return new SelfTestResult(true, "FerienMonitor", "Es sind keine Ferien geladen.");
            }
        }
    }

    /// <summary>
    /// Datenspeicher aus dem iCal Ordner
    /// </summary>
    public class FerienDateien
    {
        public IReadOnlyCollection<FerienDatei>? Items { get; private set; }
        public static FerienDateien Instance { get; } = new FerienDateien();
        private FerienDateien()
        {
        }

        /// <summary>
        /// Lädt alle ics Dateien
        /// </summary>
        public void Load(string iCalFolder)
        {
            if(!Directory.Exists(iCalFolder))
            {
                throw new InvalidOperationException("Der iCal Ordner existiert nicht: " + iCalFolder);
            }
            List<FerienDatei> lst = new List<FerienDatei>();
            foreach (string icalFile in Directory.EnumerateFiles(iCalFolder, "ferien_baden-wuerttemberg_*.ics"))
            {
                lst.Add(new FerienDatei(icalFile));
            }

            this.Items = lst.AsReadOnly();
        }
    }

    /// <summary>
    /// repräsentiert den Inhalt einer der Ferien ICS Dateien
    /// </summary>
    public class FerienDatei
    {
        private string _Filename;
        private Ical.Net.Calendar _calendar;

        /// <summary>
        /// Name der iCal / ICS Datei
        /// </summary>
        public string Name => Path.GetFileName(this._Filename);

        /// <summary>
        /// Die eingelesenen Ferienobjekte
        /// </summary>
        public IReadOnlyCollection<Ferien> Ferien { get; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="name">Dateiname einer ICS Datei</param>
        public FerienDatei(string name)
        {
            this._Filename = name;

            List<Ferien> liste = new List<Ferien>();
            using (FileStream fs = File.OpenRead(name))
            {
                Ical.Net.Calendar ical = Ical.Net.Calendar.Load(fs);
                this._calendar = ical;
                this.Ferien = Parse(ical);
            }
        }

        /// <summary>
        /// Parser für die ICS Daten im iCal Format
        /// </summary>
        /// <param name="ical"></param>
        /// <returns></returns>
        private static IReadOnlyCollection<Ferien> Parse(Calendar ical)
        {
            List<Ferien> ferien = new List<Ferien>();

            ferien.AddRange(ical.Events.Select(evt => new Ferien()
            {
                Start = evt.DtStart.AsSystemLocal,
                Ende = evt.DtEnd.AsSystemLocal,
                Name = evt.Summary
            }));

            return ferien
                .MergeByName()
                .OrderBy(item => item.Start)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Erweiterungsmethoden für Ferienlisten
    /// </summary>
    public static class FerienListExtensions
    {
        /// <summary>
        /// Fasst alle Ferien mit dem selben Namen zusammen zu einem großen Termin
        /// </summary>
        /// <param name="items">Einzelne Ferienelemente</param>
        /// <returns>Zusammengefasste Ferienelemente</returns>
        public static IEnumerable<Ferien> MergeByName(this IEnumerable<Ferien> items)
        {
            return items
                .GroupBy(item => item.Name)
                .Select(item => item.Count() == 1 ? item.First() : new Ferien()
                {
                    Name = item.Key,
                    Start = item.Select(innerItem => innerItem.Start).Min(),
                    Ende = item.Select(innerItem => innerItem.Ende).Max()
                });
        }
    }

}
