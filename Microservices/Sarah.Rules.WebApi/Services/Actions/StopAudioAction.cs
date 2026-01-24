using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System;

namespace Sarah.Rules.Actions
{
    public class StopAudioAction : RuleAction
    {
        private readonly RabbitMQClient _rabbitMQClient;
        protected string Hostname { get; }

        public StopAudioAction(string hostname, RabbitMQClient rabbitMQClient)
        {
            this._rabbitMQClient = rabbitMQClient;
            this.Hostname = hostname;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            var message = new StopAudioMessage(this.Hostname);
            _rabbitMQClient.PublishAsync(message).Wait();
        }
    }
}
