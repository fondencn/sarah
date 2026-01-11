using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.Rules.Actions
{
    public class SayAirQualityAction : RuleAction
    {
        protected readonly IEventProcessingService _events;
        
        public SayAirQualityAction(int targetNodeId, string hostname, IEventProcessingService events)
        {
            this._events = events;
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
                _events.PublishSay(new SayEvent(airEvent.Message, this.Hostname));
            }
        }
    }
}
