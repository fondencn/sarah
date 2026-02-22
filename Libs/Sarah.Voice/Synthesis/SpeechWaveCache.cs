using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sarah.Voice.Synthesis
{
    /// <summary>
    /// Cache für Spracheausgabe-Dateien, damit der selbe Text nicht mehrmals synthetisiert wird
    /// </summary>
    internal class SpeechWaveCache
    {
        #region singleton
        public static SpeechWaveCache Instance { get; } = new SpeechWaveCache();
        private SpeechWaveCache()
        {
            if(!Directory.Exists(CacheFolderName))
            {
                Directory.CreateDirectory(CacheFolderName);
            }
            LoadFromFile();
        }
        #endregion

        private const string CacheFileName = "SpeechCacheEntries.csv";
        private const string CacheFolderName = "SpeechCache";
        internal const string CsvSeparator = "|||";

        private ILogger _logger;

        public void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Der In-Memory Cache
        /// </summary>
        private Dictionary<string, FileInfo> Cache { get; } = new Dictionary<string, FileInfo>();


        /// <summary>
        /// Initialisiert den Cache aus der Datenbankdatei
        /// </summary>
        private void LoadFromFile()
        {
            try
            {
                string cacheFile = Path.Combine(CacheFolderName, CacheFileName);
                if (File.Exists(cacheFile))
                {
                    this.Cache.Clear();
                    string[] lines = File.ReadAllLines(cacheFile);
                    foreach (string line in lines)
                    {
                        string[] cols = line.Split(CsvSeparator);
                        string cachedFileName = cols[1]; //enhält bereits den Ordnernamen
                        this.Cache.Add(cols[0], new FileInfo(cachedFileName));
                        if (!File.Exists(cachedFileName))
                        {
                            string msg = "WAVE File im Cache existiert nicht: " + cachedFileName;
                            _logger?.LogError(msg);
                            throw new InvalidOperationException(msg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading speech wave cache");
            }
        }


        /// <summary>
        /// Versucht einen Cacheeintrag mit dem angegebenen Schlüssel zu ermitteln
        /// </summary>
        /// <param name="key"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool TryGet(string key, out FileInfo file) => Cache.TryGetValue(key.Sanitize(), out file);

        /// <summary>
        /// Fügt dem Cache für den angegebenen Schlüssel einen neuen Eintrag hinzu und
        /// generiert einen neuen (leeren) Dateinamen, der dann mit Inhalt gefüllt werden kann
        /// </summary>
        /// <param name="key">Synthetisierter Text</param>
        /// <returns>Neuer Dateiname im Cache, der befüllt werden soll</returns>
        public FileInfo Add(string key)
        {
            string sanitizedKey = key.Sanitize();
            FileInfo file;
            if(Cache.ContainsKey(sanitizedKey))
            {
                file = Cache[sanitizedKey];
            }
            else
            {

                string cacheFile = Path.Combine(CacheFolderName, CacheFileName);
                string waveFileName = Path.Combine(CacheFolderName, Guid.NewGuid() + ".wav");
                file = new FileInfo(waveFileName);
                Cache.Add(sanitizedKey, file);
                File.AppendAllText(cacheFile, sanitizedKey + CsvSeparator + waveFileName + Environment.NewLine);
            }
            file.Refresh();
            return file;
        }



        //private class SpeechWaveCacheEntry
        //{
        //    public string Key { get;  }
        //    public FileInfo WaveFile { get; }

        //    public SpeechWaveCacheEntry(string key, FileInfo file)
        //    {
        //        if(key == null)
        //        {
        //            throw new ArgumentNullException(nameof(key));
        //        }
        //        if (file == null)
        //        {
        //            throw new ArgumentNullException(nameof(file));
        //        }
        //        this.Key = key;
        //        this.WaveFile = file;
        //    }
        //}

    }
    internal static class KeyStringExtensions
    {

        internal static string Sanitize(this string str) =>
            str.Replace("\r", "")
            .Replace("\n", "")
            .Replace(SpeechWaveCache.CsvSeparator, "");
    }
}
