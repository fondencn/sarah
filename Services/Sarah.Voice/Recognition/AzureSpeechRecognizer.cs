using Sarah.Logging;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;
using Sarah.API.Interfaces.Service;
using Microsoft.Extensions.Configuration;

namespace Sarah.Voice.Recognition
{
    public sealed class AzureSpeechRecognizer : IDisposable
    {
        private readonly SpeechConfig _config;
        private SpeechRecognizer _recognizer;

        private ActivationKeyword Keyword { get; set; }
        private ILEDService _LEDService;
        private IConfiguration _Configuration;

        public AzureSpeechRecognizer(ILEDService ledService, IConfiguration configuration)
        {
            _LEDService = ledService;
            _Configuration = configuration;
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            // The default language is "en-us".
            _config = SpeechConfig.FromSubscription(_Configuration["AzureSpeech:SubscriptionKey"], _Configuration["AzureSpeech:Region"]);
            _config.SpeechRecognitionLanguage = "de-DE";
            _config.SpeechSynthesisLanguage = "de-DE";
            _config.EnableAudioLogging();
        }

        public event EventHandler<string> Recognized;
        //public event EventHandler Activated;


        public void Initialize()
        {
            try
            {
                _recognizer = new SpeechRecognizer(_config);
                // Creates an instance of a keyword recognition model. Update this to
                // point to the location of your keyword recognition model.

                // Datei erstellen über https://speech.microsoft.com/
                // hier ist die Anleitung:https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-create-kws


                
                this.Keyword = new ActivationKeyword("Sarah", "sarah2.table");
                Logger.Instance.LogInfo("Activation Key Word: " + this.Keyword.ModelFileName);


                _recognizer.Recognized += this._recognizer_Recognized;
                _recognizer.Canceled += this._recognizer_Canceled;
                _recognizer.Recognizing += this._recognizer_Recognizing;
                _recognizer.SessionStarted += this._recognizer_SessionStarted;
                _recognizer.SessionStopped += this._recognizer_SessionStopped;
                _recognizer.SpeechStartDetected += _recognizer_SpeechStartDetected;
                _recognizer.SpeechEndDetected += _recognizer_SpeechEndDetected;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim Initialisieren der Spracherkennung", ex);
                throw;
            }

        }

        private void _recognizer_SpeechEndDetected(object sender, RecognitionEventArgs e)
        {
            Logger.Instance.LogInfo("SPEECH: _recognizer_SpeechEndDetected");
        }

        private void _recognizer_SpeechStartDetected(object sender, RecognitionEventArgs e)
        {
            Logger.Instance.LogInfo("SPEECH: _recognizer_SpeechStartDetected");
        }

        private void _recognizer_SessionStopped(object sender, SessionEventArgs e)
        {
            Logger.Instance.LogInfo("SPEECH: _recognizer_SessionStopped");
            /* Restart recognition */
            _ = _recognizer.StartKeywordRecognitionAsync(this.Keyword.Model).ConfigureAwait(false);

        }

        private void _recognizer_SessionStarted(object sender, SessionEventArgs e)
        {
            Logger.Instance.LogInfo("SPEECH: _recognizer_SessionStarted");
        }

        private void _recognizer_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizingKeyword)
            {
                Logger.Instance.LogInfo($"RECOGNIZING KEYWORD: Text={e.Result.Text}");
            }
            else if (e.Result.Reason == ResultReason.RecognizingSpeech)
            {
                Logger.Instance.LogInfo($"RECOGNIZING: Text={e.Result.Text}");
            }
        }

        private void _recognizer_Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.Reason == CancellationReason.Error)
            {
                Logger.Instance.LogError($"SPEECH: _recognizer_Canceled: ErrorCode={e.ErrorCode}");
                Logger.Instance.LogError($"SPEECH: _recognizer_Canceled: ErrorDetails={e.ErrorDetails}");
                Logger.Instance.LogError($"SPEECH: _recognizer_Canceled: Did you update the subscription info?");
            }
            _ = _recognizer.StartKeywordRecognitionAsync(this.Keyword.Model).ConfigureAwait(false);
        }

        private void _recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                Logger.Instance.LogInfo($"SPEECH: RECOGNIZED KEYWORD: Text={e.Result.Text}");
                _LEDService.Listening();
            }
            else if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Logger.Instance.LogInfo($"SPEECH: RECOGNIZED: Text={e.Result.Text}");

                string normalizedText = NormalizeSpeechInput(e.Result.Text);
                if (!String.IsNullOrWhiteSpace(normalizedText))
                {
                    /* Verarbeitung asynchron astoßen */
                    _ = Task.Run(() => this.Recognized?.Invoke(this, normalizedText)).ConfigureAwait(false);
                }
                else
                {
                    Logger.Instance.LogInfo($"SPEECH: RecognizeOnceAsync...");
                    SpeechService.PlayNotifySound();
                    _ = this._recognizer.RecognizeOnceAsync();
                }
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Logger.Instance.LogInfo("SPEECH: NOMATCH: Speech could not be recognized.");
            }
        }

        private string NormalizeSpeechInput(string text)
        {
            if (text.StartsWith(this.Keyword.TextRepresentation, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(this.Keyword.TextRepresentation.Length);
            }
            text = text.TrimStart(',', '.', '!', '?', ' ');
            if(text.Equals("Server.", StringComparison.OrdinalIgnoreCase))
            {
                text = "";
            }

            /* Do some corrections of common misunderstandings */
            text = text.Replace("Mama Zimmer", "Mamazimmer")
                .Replace("EK Lampe", "Ecklampe")
                .Replace("Sofa Lampe", "Sofalampe")
                .Replace("Sofa Licht", "Sofalicht")
                .Replace("Licht Wohnzimmer", "Wohnzimmerlicht")
                .Replace("Fenster Licht", "Fensterlicht")
                .Replace("Küche Licht", "Küchenlicht")
                .Replace("Badezimmer Licht", "Badezimmerlicht")
                .Replace("Flur Licht", "Flurlicht")
                .Replace("Garten Licht", "Gartenlicht")
                .Replace("Schlafzimmer Licht", "Schlafzimmerlicht")
                .Replace("Esszimmer Licht", "Esszimmerlicht")
                .Replace("Büro Licht", "Bürolicht")
                .Replace("Keller Licht", "Kellerlicht");  

            return text;
        }

        public void StartListen()
        {
            Logger.Instance.LogInfo("SPEECH: Starting Keyword recognition...");
            _ = _recognizer.StartKeywordRecognitionAsync(this.Keyword.Model).ConfigureAwait(false);
        }

        public void StopListen()
        {
            _ = this._recognizer.StopKeywordRecognitionAsync().ConfigureAwait(false);
        }


        public void Dispose()
        {
            _recognizer?.Dispose();
        }

    }
}
