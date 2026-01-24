using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System;

namespace Sarah.Rules.Actions
{
    public class StartAudioAction : RuleAction
    {
        private readonly RabbitMQClient _rabbitMQClient;
        protected string AudioFileName { get; }
        protected string Hostname { get; }

        public StartAudioAction(string audioFileName, string hostname, RabbitMQClient rabbitMQClient)
        {
            this._rabbitMQClient = rabbitMQClient;
            this.AudioFileName = audioFileName;
            this.Hostname = hostname;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            var message = new StartAudioMessage(this.AudioFileName, this.Hostname);
            _rabbitMQClient.PublishAsync(message).Wait();
        }
    }
}
