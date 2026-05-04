using System;
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
            if (evt is WallPlugStateChangedEvent wp && wp.SourceNodeId == this.TargetNodeId)
                return wp.IsOn && (DateTime.Now - wp.LastChangeToPowerHigh).TotalSeconds < 60;
            return false;
        }
    }

    public class WallPlugPowerDecreasingCondition : RuleCondition
    {
        public WallPlugPowerDecreasingCondition(byte targetNodeId) : base(targetNodeId) { }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is WallPlugStateChangedEvent wp && wp.SourceNodeId == this.TargetNodeId)
                return wp.IsOn && (DateTime.Now - wp.LastChangeToPowerLow).TotalSeconds < 60;
            return false;
        }
    }
}
