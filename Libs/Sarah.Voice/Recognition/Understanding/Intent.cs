using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;

namespace Sarah.Voice.Recognition.Understanding
{
    public abstract class Intent
    {
        private static readonly string[] _SuccessResponses = new string[]
        {
            "OK", "Ja", "Erledigt", "In Ordnung"
        };
        private static readonly string[] _FailureResponses = new string[]
        {
            "Das hat leider nicht funktioniert", "Es gab ein Problem", "Das konnte ich leider nicht machen"
        };


        public virtual string HelpText => "Hierzu ist leider keine Hilfe verfügbar.";

        private Random _rng = new Random(747644);

        protected ISpeechService SpeechService { get; }

        protected string Location => this.SpeechService.Location;

        protected IDeviceService _DeviceServiceClient;
        
        protected ILogger _logger;

        public Intent (ISpeechService speechService, IDeviceService deviceServiceClient, ILogger logger)
        {
            this.SpeechService = speechService;
            this._DeviceServiceClient = deviceServiceClient;
            this._logger = logger;
        }

        protected void Say(string speechOutput)
        {
            this.SpeechService.Say(speechOutput);
        }

        protected void SaySuccess()
        {
            int r = _rng.Next(0, _SuccessResponses.Length);
            this.Say(_SuccessResponses[r]);
        }

        protected void SayFail()
        {
            int r = _rng.Next(0, _FailureResponses.Length);
            this.Say(_FailureResponses[r]);
        }

        public virtual string Title => this.GetType().Name;

        public bool CanHandle(string recognizedSpeech) => CanHandleInternal(Normalize(recognizedSpeech));

        protected abstract bool CanHandleInternal(string recognizedSpeech);

        public void Handle(string recognizedSpeech) => HandleInternal(Normalize(recognizedSpeech));

        protected abstract void HandleInternal(string recognizedSpeech);



        protected static string Normalize(string recognizedSpeech)
        {
            return (recognizedSpeech ?? String.Empty)
                .Trim()
                .Trim('.')
                .Replace(",", String.Empty)
                //.Replace(".", String.Empty)
                .Replace("?", String.Empty)
                .Replace("!", String.Empty)
                .Replace("-", String.Empty)
                .Replace("|", String.Empty)
                .Replace("*", String.Empty)
                .Replace("Mama Zimmer", "Mamazimmer")
                .Trim();
        }
    }
}
