using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System;

namespace Sarah.Rules.Actions
{
    public class StopAudioAction : RuleAction
    {
        private readonly IEventProcessingService _events;
        protected string Hostname { get; }

        public StopAudioAction(string hostname, IEventProcessingService events)
        {
            this._events = events;
            this.Hostname = hostname;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            _events.PublishStopPlayAudioEventAsync(new StopAudioEvent(this.Hostname));
        }
    }
}
