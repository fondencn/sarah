using Sarah.API.BusinessObjects.SpeakerRequests;
using Sarah.Logging;
using Services.Sarah.API.Interfaces.Service;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class OpenDoorIntent : Intent
    {
        public override string Title => "Türstatus abfragen";
        public override string HelpText => "Sag \"Welche Türen sind offen?\"";

        private Regex PatternMatchExpression { get; }


        public OpenDoorIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient) : base(speechService, deviceServiceClient)
        {
            string matchPattern = "^.*Welche Türen sind offen$|^.*Welche Türen sind geöffnet$|^.*Sind alle Türen zu|^.*Sind alle Türen geschlossen$";
            this.PatternMatchExpression = new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }


        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return PatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected override async void HandleInternal(string recognizedSpeech)
        {
            try
            {
                GetOpenDoorsResponse response = await _DeviceServiceClient.GetOpenDoors();
                this.Say(response.OpenDoorInfo);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
                this.SayFail();
            }
        }
    }
}
