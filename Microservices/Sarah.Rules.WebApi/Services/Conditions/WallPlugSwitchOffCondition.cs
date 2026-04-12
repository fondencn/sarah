using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class WallPlugSwitchOffCondition : RuleCondition
    {
        public WallPlugSwitchOffCondition(byte targetNodeId) : base(targetNodeId) { }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is WallPlugStateChangedEvent wp && wp.SourceNodeId == this.TargetNodeId)
                return !wp.IsOn;
            return false;
        }
    }
}
