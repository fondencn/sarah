using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.Rules.Actions
{
    public class SayAirQualityAction : RuleAction
    {
        protected readonly RabbitMQClient _rabbitMQClient;
        
        public SayAirQualityAction(int targetNodeId, string hostname, RabbitMQClient rabbitMQClient)
        {
            this._rabbitMQClient = rabbitMQClient;
            TargetNodeId = targetNodeId;
            Hostname = hostname;
        }

        public int TargetNodeId { get; }
        public string Hostname { get; }

        public override void Execute(NetworkEvent sourceEvent)
        {
            AirQualityChangedEvent airEvent = sourceEvent as AirQualityChangedEvent;

            if (airEvent != null && airEvent.SourceNodeId == TargetNodeId)
            {
                var message = new SayMessage(airEvent.Message, this.Hostname);
                _rabbitMQClient.PublishAsync(message).Wait();
            }
        }
    }
}
