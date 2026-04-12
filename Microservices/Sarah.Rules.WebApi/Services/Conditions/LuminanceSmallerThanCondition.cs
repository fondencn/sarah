using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class LuminanceSmallerThanCondition : RuleCondition
    {
        private int LuminanceLimit { get; set; }

        public LuminanceSmallerThanCondition(byte targetNodeId, int luminanceLimit) : base(targetNodeId)
        {
            this.LuminanceLimit = luminanceLimit;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is MultiSensorStateChangedEvent ms && ms.SourceNodeId == this.TargetNodeId && ms.Luminance.HasValue)
                return ms.Luminance.Value < this.LuminanceLimit;
            return false;
        }
    }
}
