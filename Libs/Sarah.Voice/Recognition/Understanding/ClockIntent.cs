using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces.Services;
using System;
using System.Text.RegularExpressions;

namespace Sarah.Voice.Recognition.Understanding
{
    public class ClockIntent : Intent
    {

        public override string Title => "Uhrzeit";
        public override string HelpText => "Sag \"Wie spät ist es?\", dann sage ich dir die Uhrzeit";



        private Regex PatternMatchExpression { get; }

        public ClockIntent(SpeechService speechService, IDeviceService deviceServiceClient, ILogger<ClockIntent> logger) : base(speechService, deviceServiceClient, logger)
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
                _logger.LogError(ex, "Error in ClockIntent");
                this.SayFail();
            }
        }
    }
}
