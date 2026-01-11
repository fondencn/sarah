using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces.Service;
using Services.Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Recognition.Understanding
{
    public class Intents
    {
        internal List<Intent> KnownIntents { get; }

        #region Singleton
        private SpeechService SpeechService { get; }
        public static Intents Instance { get; private set; }
        private Intents(SpeechService speechService, IDeviceServiceClient deviceServiceClient, ILEDService lEDService, ILoggerFactory loggerFactory)
        {
            this.SpeechService = speechService;
            this.KnownIntents = IntentFactory.CreateIntents(speechService , deviceServiceClient, lEDService, loggerFactory);
        }
        public static void Initialize(SpeechService speechService, IDeviceServiceClient deviceServiceClient, ILEDService lEDService, ILoggerFactory loggerFactory)
        {
            Instance = new Intents(speechService, deviceServiceClient, lEDService, loggerFactory);
        }

        #endregion


        internal Task Handle(string recognizedText)
        {
            Intent match = this.KnownIntents.First(item => item.CanHandle(recognizedText));
            match.Handle(recognizedText);

            return Task.CompletedTask;
        }
    }
}
