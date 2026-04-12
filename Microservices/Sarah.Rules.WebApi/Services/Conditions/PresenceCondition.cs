using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class PresenceCondition : RuleCondition
    {
        public bool Value { get; }

        public PresenceCondition(byte targetNodeId, bool targetValue) : base(targetNodeId)
        {
            this.Value = targetValue;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is MultiSensorStateChangedEvent ms && ms.SourceNodeId == this.TargetNodeId && ms.Presence.HasValue)
                return (ms.Presence.Value > 0) == this.Value;
            return false;
        }
    }
}
