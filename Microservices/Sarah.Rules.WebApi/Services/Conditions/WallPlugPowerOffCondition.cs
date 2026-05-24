using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class WallPlugPowerOffCondition : RuleCondition
    {
        public WallPlugPowerOffCondition(byte targetNodeId) : base(targetNodeId) { }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is WallPlugPowerLowEvent wp && wp.SourceNodeId == this.TargetNodeId)
                return true;
            return false;
        }
    }
}
