using Sarah.API.BusinessObjects.SpeakerRequests;
using Sarah.Logging;
using Services.Sarah.API.Interfaces.Service;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class CoronaIntent : Intent
    {

        public override string Title => "Sars-COV-19 Infos";
        public override string HelpText => "Sag \"Wie sind die aktuellen Corona-Zahlen?\" für Informationen zu Sars-Cov-19. ";



        private Regex PatternMatchExpression { get; }

        public CoronaIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient) : base(speechService, deviceServiceClient)
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
                Logger.Instance.LogException("CoronaIntent", ex);
                this.SayFail();
            }
        }
    }
}
