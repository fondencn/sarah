using System;
using Services.Sarah.API.Interfaces.Service;

namespace Sarah.Voice.Recognition.Understanding
{
    public class NoMatchIntent : Intent
    {
        public override string Title => "";


        private static string[] Answers = new string[]
        {
            "Das habe ich leider nicht verstanden.",
            "Oh, tut mir leid, wie war das bitte?",
            "Ich verstehe leider nicht ganz, was du meinst.",
            "Tut mir leid, da kann ich dir leider nicht weiter helfen.",
            "Oh, das bringt meine künstliche Intelligenz ans Limit!"
        };

        private Random _rng = new Random();


        public NoMatchIntent(SpeechService speechService, IDeviceServiceClient deviceServiceClient) : base(speechService, deviceServiceClient)
        {
        }

        protected override void HandleInternal(string recognizedSpeech)
        {
            int r = _rng.Next(0, Answers.Length);
            this.Say(Answers[r]);
        }

        protected override bool CanHandleInternal(string recognizedSpeech) => !String.IsNullOrWhiteSpace(recognizedSpeech);
    }
}
