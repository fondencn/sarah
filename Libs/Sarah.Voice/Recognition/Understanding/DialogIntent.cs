using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Services.Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Recognition.Understanding
{
    public class DialogIntent : Intent
    {
        public override string Title => "Unterhalten";
        public override string HelpText => "Du kannst dich auch einfach mit mir unterhalten. ";


        private string InputPattern { get; }
        private string[] Outputs { get; }

        private Func<string> OutputFunc { get; }

        private Regex PatternMatchExpression { get; }

        private Random _rng = new Random();

        public DialogIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient, ILogger<DialogIntent> logger, string inputPattern, params string[] outputs) : base(speechService, deviceServiceClient, logger)
        {
            this.InputPattern = inputPattern;
            this.Outputs = outputs;
            this.OutputFunc = null;
            this.PatternMatchExpression = new Regex(inputPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        public DialogIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient, ILogger<DialogIntent> logger, string inputPattern, Func<string> outFunc) : base(speechService, deviceServiceClient, logger)
        {
            this.InputPattern = inputPattern;
            this.Outputs = null;
            this.OutputFunc = outFunc;
            this.PatternMatchExpression = new Regex(inputPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected override void HandleInternal(string recognizedSpeech)
        {
            if (OutputFunc != null)
            {
                this.Say(OutputFunc());
            }
            else
            {
                int answerIndex = _rng.Next(0, Outputs.Length);
                this.Say(Outputs[answerIndex]);
            }
        }

        protected override bool CanHandleInternal(string recognizedSpeech)
        {
            return this.PatternMatchExpression.IsMatch(recognizedSpeech);
        }
    }
}
