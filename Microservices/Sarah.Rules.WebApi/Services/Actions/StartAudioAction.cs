using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System;

namespace Sarah.Rules.Actions
{
    public class StartAudioAction : RuleAction
    {
        private readonly IEventProcessingService _events;
        protected string AudioFileName { get; }
        protected string Hostname { get; }

        public StartAudioAction(string audioFileName, string hostname, IEventProcessingService events)
        {
            this._events = events;
            this.AudioFileName = audioFileName;
            this.Hostname = hostname;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            _events.PublishStartPlayAudioEventAsync(new StartAudioEvent(this.AudioFileName, this.Hostname));
        }
    }
}
