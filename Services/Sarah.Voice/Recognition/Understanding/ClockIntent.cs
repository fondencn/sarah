using Sarah.Logging;
using Services.Sarah.API.Interfaces.Service;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class ClockIntent : Intent
    {

        public override string Title => "Uhrzeit";
        public override string HelpText => "Sag \"Wie spät ist es?\", dann sage ich dir die Uhrzeit";



        private Regex PatternMatchExpression { get; }

        public ClockIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient) : base(speechService, deviceServiceClient)
        {
            string matchPattern = "^.*Wie spät ist es.*$";
            this.PatternMatchExpression = new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected override void HandleInternal(string recognizedSpeech)
        {
            try
            {
                this.Say("Es ist " + DateTime.Now.Hour + " Uhr " + DateTime.Now.Minute);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("JokeIntent", ex);
                this.SayFail();
            }
        }
    }
}
