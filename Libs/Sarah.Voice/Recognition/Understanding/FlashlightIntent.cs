using System;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces.Services;
using Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Recognition.Understanding
{
    public class FlashlightIntent : Intent
    {
        public override string Title => "Taschenlampe";
        public override string HelpText => "Sag \"Taschenlampe an...\" oder \"Taschenlampe aus... \" ";


        private Regex PatternMatchExpression { get; }
        private ILEDService LEDService { get; }

        public FlashlightIntent(SpeechService speechService, IDeviceService deviceServiceClient, ILEDService ledService, ILogger<FlashlightIntent> logger) : base(speechService, deviceServiceClient, logger)
        {
            this.LEDService = ledService;
            string matchPattern = "^.*Taschenlampe (an|aus).*$";
            this.PatternMatchExpression = new Regex(matchPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech);
        }

        protected override void HandleInternal(string recognizedSpeech)
        {
            Match m = this.PatternMatchExpression.Match(recognizedSpeech);
            bool isOn = String.Equals(m.Groups[1].Value, "an", StringComparison.OrdinalIgnoreCase);

            //if(isOn)
            //{
            //    Process.Start("sudo", " ./hub-ctrl/hub-ctrl -H 1 -P 2 -p 1");
            //} 
            //else
            //{
            //    Process.Start("sudo", " ./hub-ctrl/hub-ctrl -H 1 -P 2 -p 0");
            //}
            this.SaySuccess();

            Thread.Sleep(4000);

            this.LEDService.FlashLight(isOn);
        }
    }
}
