using Sarah.API.BusinessObjects;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Synthesis
{

    // HINWEIS: Rest implementierung: https://docs.microsoft.com/de-de/azure/cognitive-services/speech-service/rest-text-to-speech


    public sealed class AzureSpeechSynthesizer : IDisposable
    {
        private SpeechConfig _config;
        private ILEDService _LEDService;
        
        private IConfiguration _Configuration;
        private ILogger<AzureSpeechSynthesizer> _logger;

        /// <summary>
        /// ctor
        /// </summary>
        public AzureSpeechSynthesizer(ILEDService ledService, IConfiguration configuration, ILogger<AzureSpeechSynthesizer> logger)
        {
            _LEDService = ledService;
            _Configuration = configuration;
            _logger = logger;

            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            // The default language is "en-us".
            _config = SpeechConfig.FromSubscription(_Configuration["Speech:SubscriptionKey"], _Configuration["Speech:Region"]);
            //config.SpeechRecognitionLanguage = "de-DE";
            _config.SpeechSynthesisLanguage = "de-DE";
            _config.SpeechSynthesisVoiceName = "de-DE-KatjaNeural";
            // _config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);



        }


        public void Say(string text, SpeechVolume vol) => SynthesisToSpeakerAsync(text, vol);

        private bool _isSpeaking;

        private void SynthesisToSpeakerAsync(string text, SpeechVolume vol)
        {

            if (!this._isSpeaking)
            {
                this._isSpeaking = true;
                try
                {
                    /* LED Animation */
                    _ = _LEDService.Speaking();


                    FileInfo cacheFile;
                    if (!SpeechWaveCache.Instance.TryGet(text, out cacheFile))
                    {
                        cacheFile = SpeechWaveCache.Instance.Add(text);

                        /* Linux: Via Wave File beacuse of crackling alsa/pulseaudio output on audio jack */
                        AudioConfig audioConfig = AudioConfig.FromWavFileOutput(cacheFile.FullName);
                        SpeechSynthesizer synthesizer = new SpeechSynthesizer(_config, audioConfig);


                        if (text.StartsWith("<speak>", StringComparison.OrdinalIgnoreCase))
                        {
                            string ssml = text.Replace("<speak>", "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"de-DE\"><voice name=\"de-DE-KatjaNeural\">");
                            ssml = ssml.Replace("</speak>", "</voice></speak>");

                            _logger.LogDebug("before  SpeakSsmlAsync");
                            using (SpeechSynthesisResult result = synthesizer.SpeakSsmlAsync(ssml).Result)
                            {
                                if (result.Reason == ResultReason.Canceled)
                                {
                                    _logger.LogDebug("after SpeakSsmlAsync: Reason is {Reason}", result.Reason);
                                    SpeechSynthesisCancellationDetails cancellationCause = SpeechSynthesisCancellationDetails.FromResult(result);
                                    _logger.LogError("{ErrorDetails}", cancellationCause.ErrorDetails);
                                    _logger.LogError("{Ssml}", ssml);
                                }
                                else
                                {
                                    _logger.LogDebug("after SpeakSsmlAsync: Reason is {Reason}", result.Reason);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogDebug("before  SpeakTextAsync");
                            using (SpeechSynthesisResult result = synthesizer.SpeakTextAsync(text).Result)
                            {
                                _logger.LogDebug("after SpeakTextAsync: Reason is {Reason}", result.Reason);
                            }


                            // CF 21.02.2021: Beim Dispose des Objekts momentan NativeException und Absturz
                            //Logger.Instance.LogDebug("before _synthesizer.Dispose");
                            //synthesizer.Dispose();
                            //audioConfig.Dispose();
                            //Logger.Instance.LogDebug("after _synthesizer.Dispose");
                        }
                    }

                    using (SpeechVolumeScope volScope = new SpeechVolumeScope(vol))
                    {
                        SpeechService.PlaySound(cacheFile.FullName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei der Sprachausgabe");
                }
                finally
                {
                    _logger.LogDebug("Finally - Not Speaking anymore");
                    this._isSpeaking = false;
                }
            }
            else
            {
                _logger.LogDebug("SPEECH: Already Speaking, ommitting \"{Text}\"", text);
            }
        }

        public void Dispose()
        {
            //this._audioConfig?.Dispose();
            //this._audioConfig = null;
        }


        private sealed class SpeechVolumeScope : IDisposable
        {
            private int _ChangedPercetance = 0;

            public SpeechVolumeScope(SpeechVolume volumeLevel)
            {
                switch (volumeLevel)
                {
                    case SpeechVolume.Quieter:
                        _ChangedPercetance = -20;
                        break;
                    case SpeechVolume.Louder:
                        _ChangedPercetance = +20;
                        break;
                    case SpeechVolume.VeryLoud:
                        _ChangedPercetance = +40;
                        break;
                    case SpeechVolume.Normal:
                    default:
                        _ChangedPercetance = 0;
                        break;
                }

                if (_ChangedPercetance != 0)
                {
                    SetMasterVolume(_ChangedPercetance);
                }
            }

            private static void SetMasterVolume(int percentage)
            {
                Debug.WriteLine("SetMasterVolume percentage=" + percentage);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    /* Linux ALSA amixer verwenden */
                    string soundDeviceName = GetDefaultDevice();
                    string amixer = $"amixer";
                    string amixerArgs = $" set {soundDeviceName} {Math.Abs(percentage)}%{(percentage < 0 ? "-" : "+")}";
                    ProcessStartInfo pi = new ProcessStartInfo(amixer, amixerArgs);
                    using (Process p = Process.Start(pi))
                    {
                        Console.WriteLine("Running amixer " + amixerArgs);
                        p.WaitForExit();
                    }
                }
                else
                {
                    /* Unter Windows nicht implementiert */
                    //AudioSwitcher.AudioApi.CoreAudio.CoreAudioDevice defaultPlaybackDevice
                    //    = new AudioSwitcher.AudioApi.CoreAudio.CoreAudioController().DefaultPlaybackDevice;
                    //defaultPlaybackDevice.Volume = defaultPlaybackDevice.Volume + (defaultPlaybackDevice.Volume * percentage * 0.01);
                }
            }

            private static string GetDefaultDevice()
            {
                if (File.Exists("default_snd_dev.cfg"))
                {
                    return File.ReadAllText("default_snd_dev.cfg").Trim();
                }
                else
                {
                    return "Master";
                }
            }

            public void Dispose()
            {
                if (_ChangedPercetance != 0)
                {
                    SetMasterVolume(_ChangedPercetance * -1);
                }
            }
        }
    }
}
