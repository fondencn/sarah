using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class WallPlugPowerIncreasingCondition : RuleCondition
    {
        public WallPlugPowerIncreasingCondition(byte targetNodeId) : base(targetNodeId) { }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is WallPlugPowerHighEvent wp && wp.SourceNodeId == this.TargetNodeId)
                return true;
            return false;
        }
    }

    public class WallPlugPowerDecreasingCondition : RuleCondition
    {
        public WallPlugPowerDecreasingCondition(byte targetNodeId) : base(targetNodeId) { }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is WallPlugPowerLowEvent wp && wp.SourceNodeId == this.TargetNodeId)
                return true;
            return false;
        }
    }
}
