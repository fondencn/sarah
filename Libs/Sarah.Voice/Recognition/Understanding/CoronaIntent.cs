using Sarah.API.BusinessObjects.SpeakerRequests;
using Services.Sarah.API.Interfaces.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class CoronaIntent : Intent
    {

        public override string Title => "Sars-COV-19 Infos";
        public override string HelpText => "Sag \"Wie sind die aktuellen Corona-Zahlen?\" für Informationen zu Sars-Cov-19. ";



        private Regex PatternMatchExpression { get; }

        public CoronaIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient, ILogger<CoronaIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            string matchPattern = "^.*Corona.*$";
            this.PatternMatchExpression = new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected override async void HandleInternal(string recognizedSpeech)
        {
            try
            {
                GetDeseaseInfoResponse response = await _DeviceServiceClient.GetDeseaseInfo();
                this.Say(String.IsNullOrWhiteSpace(response.DiseaseInfos) ? "Es liegen keine Coronawarnungen vor." : response.DiseaseInfos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CoronaIntent");
                this.SayFail();
            }
        }
    }
}
