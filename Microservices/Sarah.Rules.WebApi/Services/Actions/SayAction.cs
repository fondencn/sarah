using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System;

namespace Sarah.Rules.Actions
{
    public class SayAction : RuleAction
    {
        protected readonly IEventProcessingService _events;
        protected string Text { get; }
        protected string Hostname { get; }
        protected Func<string> TextExpr { get; }
        private SpeechVolume Volume { get; set; } = SpeechVolume.Normal;

        public SayAction(string text, IEventProcessingService events, SpeechVolume volume = SpeechVolume.Normal)
        {
            this._events = events;
            this.TextExpr = null;
            this.Text = text;
            this.Hostname = "";
            this.Volume = volume;
        }

        public SayAction(string text, string hostname, IEventProcessingService events, SpeechVolume volume = SpeechVolume.Normal)
        {
            this._events = events;
            this.TextExpr = null;
            this.Text = text;
            this.Hostname = hostname;
            this.Volume = volume;
        }

        public SayAction(Func<string> textExpr, string hostname, IEventProcessingService events, SpeechVolume volume = SpeechVolume.Normal)
        {
            this._events = events;
            this.Text = null;
            this.TextExpr = textExpr;
            this.Hostname = hostname;
            this.Volume = volume;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            string textToSay = this.Text ?? this.TextExpr?.Invoke();
            _events.PublishSay(new SayEvent(textToSay, this.Hostname, this.Volume));
        }
    }

    public class SayOnceAction : SayAction
    {
        private DateTime LastSay { get; set; }
        public TimeSpan SilentTime { get; }
        private SpeechVolume Volume { get; set; } = SpeechVolume.Normal;


        public SayOnceAction(Func<string> textExpr, IEventProcessingService events, TimeSpan silentTime, SpeechVolume volume = SpeechVolume.Normal) : base(textExpr, "", events, volume)
        {
            this.SilentTime = silentTime;
        }

        public SayOnceAction(Func<string> textExpr, string hostname, IEventProcessingService events, TimeSpan silentTime, SpeechVolume volume = SpeechVolume.Normal) : base(textExpr, hostname, events, volume)
        {
            this.SilentTime = silentTime;
        }


        public SayOnceAction(string text, string hostname, IEventProcessingService events, TimeSpan silentTime, SpeechVolume volume = SpeechVolume.Normal) : base(text, hostname, events, volume)
        {
            this.SilentTime = silentTime;
        }



        public override void Execute(NetworkEvent sourceEvent)
        {
            if ((DateTime.Now - this.LastSay) > this.SilentTime)
            {
                string textToSay = this.Text ?? this.TextExpr?.Invoke();
                base._events.PublishSay(new SayEvent(textToSay, this.Hostname, this.Volume));
                this.LastSay = DateTime.Now;
            }
        }
    }
}
