using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.Voice.Recognition;
using Sarah.Voice.Recognition.Understanding;
using Sarah.Voice.Synthesis;
using Services.Sarah.API.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sarah.Voice
{


    /// <summary>
    /// Sprach- Ein und Ausgabe
    /// </summary>
    public class SpeechService : ISpeechService
    {
        /// <summary>
        /// Der Erkenner
        /// </summary>
        private AzureSpeechRecognizer Recognizer { get; set; }
        /// <summary>
        /// Der Sprecher
        /// </summary>
        private AzureSpeechSynthesizer Synthesizer { get; set; }

        /// <summary>
        /// Liste mt erkannten Texten
        /// </summary>
        public List<SpeechInfo> RecognizedTexts { get; } = new List<SpeechInfo>();

        /// <summary>
        /// Liste mit wiedergegebenen Texten
        /// </summary>
        public List<SpeechInfo> SynthesizedTexts { get; } = new List<SpeechInfo>();

        /// <summary>
        /// Gibt an, wo sich dieses Gerät befindet
        /// </summary>
        public string Location { get; private set; }

        /// <summary>
        /// Gibt ob, ob die Spracheingabe aktiviert ist
        /// </summary>
        private bool IsRecognitionEnabled { get; set; }

        /// <summary>
        /// Gibt ob, ob die Sprachausgabe aktiviert ist
        /// </summary>
        private bool IsSynthesisEnabled { get; set; }

        /// <summary>
        /// Ende der Still-Zeit für dieses Gerät, sofern momentan aktiviert, sonst null
        /// </summary>
        private DateTime? EndOfSilentTime { get; set; }

        /// <summary>
        /// gibt an, ob die Sprachausgabe momentan stumm geschaltet ist
        /// </summary>
        public bool IsSilent => EndOfSilentTime.HasValue && DateTime.Now < EndOfSilentTime.Value;

        private ILEDService _LEDService;

        private IDeviceServiceClient _DeviceServiceClient;
        private IWeatherProvider _weatherProvider;
        private IConfiguration _Configuration;
        private ILogger<SpeechService> _logger;
        private ILoggerFactory _loggerFactory;

        public SpeechService(ILEDService ledService, IDeviceServiceClient deviceServiceClient, IWeatherProvider weatherProvider, IConfiguration configuration, ILogger<SpeechService> logger, ILoggerFactory loggerFactory)
        {
            this._Configuration = configuration;
            this._LEDService = ledService;
            this._DeviceServiceClient = deviceServiceClient;
            this._weatherProvider = weatherProvider;
            this._logger = logger;
            this._loggerFactory = loggerFactory;
            this.Location = "Unbekannt";
            this.IsRecognitionEnabled = true;
            this.IsSynthesisEnabled = true;

        }

        /// <summary>
        /// Initialisiert den Sprachdienst
        /// </summary>
        /// <param name="location">Der Ort an dem sich dieses Gerät befindet</param>
        /// <param name="isRecognitionEnabled">Gibt an, ob die Spracherkennung aktiviert sein soll</param>
        /// <param name="isSynthesisEnabled">Gibt an, ob die Sprachausgabe aktiviert sein soll</param>
        /// <returns>Task</returns>
        public void Initialize(string location, bool isRecognitionEnabled = true, bool isSynthesisEnabled = true)
        {
            this.Location = location;
            this.IsRecognitionEnabled = isRecognitionEnabled;
            this.IsSynthesisEnabled = isSynthesisEnabled;

            if (isRecognitionEnabled)
            {
                Intents.Initialize(this, this._DeviceServiceClient, this._LEDService, this._weatherProvider, this._loggerFactory);
                this.Recognizer = new AzureSpeechRecognizer(this._LEDService, this._Configuration, _loggerFactory.CreateLogger<AzureSpeechRecognizer>());
                this.Recognizer.Initialize();
                this.Recognizer.Recognized += this.Recognizer_Recognized;
                this.Recognizer.StartListen();
            }

            if (isSynthesisEnabled)
            {
                this.Synthesizer = new AzureSpeechSynthesizer(this._LEDService, this._Configuration, _loggerFactory.CreateLogger<AzureSpeechSynthesizer>());
                //await this.Say(Greeting.Current);
                //PlayBootSound();
            }


            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            //{
            //    BeginBluetoothKeepAlive();
            //}

            _logger.LogInformation("SpeechService initialisiert. Standort: {Location}, isRecognitionEnabled: {IsRecognitionEnabled}, isSynthesisEnabled: {IsSynthesisEnabled}", location, isRecognitionEnabled, isSynthesisEnabled);
        }

        //private async void BeginBluetoothKeepAlive()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            PlaySound(Path.Combine("..", "empty.wav"));
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Instance.LogDebug("BeginBluetoothKeepAlive - " + ex.Message);
        //        }

        //        await Task.Delay(TimeSpan.FromMinutes(2));
        //    }
        //}

        /// <summary>
        ///
        /// </summary>
        public static void PlayBootSound()
        {
            string sound = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "sound_1.wav");
            PlaySound(sound);
        }

        /// <summary>
        ///
        /// </summary>
        public static void PlayNotifySound()
        {
            string sound = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "sound_10.wav");
            PlaySound(sound);
        }

        /// <summary>
        /// Spiel die angegeben Wave Datei auf einem plattformabhängigen Player ab.
        /// </summary>
        /// <param name="sound">Pfad zu einer Wavedatei</param>
        public static void PlaySound(string sound)
        {
            try
            {
                FileInfo soundFile = new FileInfo(sound);
                if (soundFile.Exists && soundFile.Extension.ToLower() == ".wav")
                {

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        /* Linux ALSA aplay verwenden (-N steht für Non-Blocking) */
                        ProcessStartInfo pi = new ProcessStartInfo("aplay", " -N" + " " + soundFile.FullName);
                        using (Process p = Process.Start(pi))
                        {
                            p.WaitForExit();
                        }
                    }
                    else
                    {
                        /* Windows: NetFramework Media.SoundPlayer Klasse via Powershell benutzen, damit wir NetCore kompatibel bleiben */
                        using (Process p = Process.Start("powershell", $"-c (New-Object Media.SoundPlayer '{soundFile.FullName}').PlaySync();"))
                        {
                            p.WaitForExit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot play sound file: {ex.Message}");
            }
        }





        private async void Recognizer_Recognized(object sender, string recognizedText)
        {
            this.RecognizedTexts.Add(new SpeechInfo("👂", recognizedText));
            await Intents.Instance.Handle(recognizedText);
        }

        /// <summary>
        /// Sagt etwas auf dem lokalen Gerät mit normaler Lautstärke
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public void Say(string text) => SayWithVolume(text, SpeechVolume.Normal);


        /// <summary>
        /// Sagt etwas auf dem lokalen Gerät mit der angegebenen Lautstärke
        /// </summary>
        /// <param name="text"></param>
        /// <param name="vol"></param>
        public void SayWithVolume(string text, SpeechVolume vol)
        {
            if (this.IsSynthesisEnabled)
            {
                if (this.IsSilent)
                {
                    _logger.LogWarning("Dieses Gerät ist gerade still bis {EndTime}", this.EndOfSilentTime.Value);
                }
                else
                {
                    this.EndOfSilentTime = null; // zeit abgelaufen
                    this.SynthesizedTexts.Add(new SpeechInfo("🔊", text));
                    Synthesizer.Say(text, vol);
                }
            }
        }


        /// <summary>
        /// Startet das zuhören
        /// </summary>
        /// <returns></returns>
        public void StartListening()
        {
            try
            {
                if (this.IsRecognitionEnabled)
                {
                    this.Recognizer.StartListen();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Fehler beim Start der Spracheingabe: {Message}", ex.Message);
            }
        }

        public void DeactivateSilentTime()
        {
            this.EndOfSilentTime = null;
        }

        public void ActivateSilentTime(DateTime dateTime)
        {
            if(dateTime > DateTime.Now)
            {
                this.EndOfSilentTime = dateTime;
            }
        }


        private Task AudioTask { get; set; }
        private CancellationTokenSource AudiocancellationTokenSource { get; set; }

        public void StartPlaySound(string audioFileName)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            AudiocancellationTokenSource = cts;
            Task t = Task.Run(() => {
                try
                {
                    while (!AudiocancellationTokenSource.Token.IsCancellationRequested)   
                    { 
                        PlaySound(audioFileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in audio playback loop");
                }
            }, AudiocancellationTokenSource.Token);
            AudioTask = t;
        }

        public void StopPlaySound()
        {
            AudiocancellationTokenSource?.Cancel();
            AudiocancellationTokenSource = null;
            AudioTask = null;
        }
    }
}
