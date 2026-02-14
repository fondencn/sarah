using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System;
using SpeechVolume = Sarah.Messaging.RabbitMQ.Messages.SpeechVolume;

namespace Sarah.Rules.Actions
{
    public class SayAction : RuleAction
    {
        protected readonly RabbitMQClient _rabbitMQClient;
        protected string? Text { get; }
        protected string Hostname { get; }
        protected Func<string>? TextExpr { get; }
        private SpeechVolume Volume { get; set; } = SpeechVolume.Normal;

        public SayAction(string text, RabbitMQClient rabbitMQClient, SpeechVolume volume = SpeechVolume.Normal)
        {
            this._rabbitMQClient = rabbitMQClient;
            this.TextExpr = null;
            this.Text = text;
            this.Hostname = "";
            this.Volume = volume;
        }

        public SayAction(string text, string hostname, RabbitMQClient rabbitMQClient, SpeechVolume volume = SpeechVolume.Normal)
        {
            this._rabbitMQClient = rabbitMQClient;
            this.TextExpr = null;
            this.Text = text;
            this.Hostname = hostname;
            this.Volume = volume;
        }

        public SayAction(Func<string> textExpr, string hostname, RabbitMQClient rabbitMQClient, SpeechVolume volume = SpeechVolume.Normal)
        {
            this._rabbitMQClient = rabbitMQClient;
            this.Text = null;
            this.TextExpr = textExpr;
            this.Hostname = hostname;
            this.Volume = volume;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            string? textToSay = this.Text ?? this.TextExpr?.Invoke();
            if (!string.IsNullOrEmpty(textToSay))
            {
                var message = new SayMessage(textToSay, this.Hostname, this.Volume);
                _rabbitMQClient.PublishAsync(message).Wait();
            }
        }
    }

    public class SayOnceAction : SayAction
    {
        private DateTime LastSay { get; set; }
        public TimeSpan SilentTime { get; }
        private SpeechVolume Volume { get; set; } = SpeechVolume.Normal;

        public SayOnceAction(string text, string hostname, RabbitMQClient rabbitMQClient, TimeSpan silentTime, SpeechVolume volume = SpeechVolume.Normal) : base(text, hostname, rabbitMQClient, volume)
        {
            this.SilentTime = silentTime;
        }

        public SayOnceAction(Func<string> textExpr, string hostname, RabbitMQClient rabbitMQClient, TimeSpan silentTime, SpeechVolume volume = SpeechVolume.Normal) : base(textExpr, hostname, rabbitMQClient, volume)
        {
            this.SilentTime = silentTime;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            if ((DateTime.Now - this.LastSay) > this.SilentTime)
            {
                string? textToSay = this.Text ?? this.TextExpr?.Invoke();
                if (!string.IsNullOrEmpty(textToSay))
                {
                    var message = new SayMessage(textToSay, this.Hostname, this.Volume);
                    _rabbitMQClient.PublishAsync(message).Wait();
                    this.LastSay = DateTime.Now;
                }
            }
        }
    }
}
