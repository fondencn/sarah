using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class AlertCondition : RuleCondition
    {
        public AlertCondition(byte targetNodeId) : base(targetNodeId) { }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is SmokeSensorAlertEvent smoke && smoke.SourceNodeId == this.TargetNodeId)
                return smoke.AlarmActive;
            return false;
        }
    }
}
