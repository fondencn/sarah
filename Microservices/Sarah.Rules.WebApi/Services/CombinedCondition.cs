using Sarah.API.BusinessObjects;
using System.Collections.Generic;
using System.Linq;

namespace Sarah.Rules
{
    public class CombinedCondition : RuleCondition
    {
        public CombinedCondition(byte targetNodeId) : this(targetNodeId, ConditionOperator.AND, null!)
        {
        }
        public CombinedCondition(byte targetNodeId, ConditionOperator oper, params RuleCondition[]? conditions) : base(targetNodeId)
        {
            this.Operator = oper;
            if (conditions != null)
            {
                this.Conditions.AddRange(conditions);
            }
        }

        public ConditionOperator Operator { get; set; }
        public List<RuleCondition> Conditions { get; } = new List<RuleCondition>();


        public override bool Evaluate(NetworkEvent evt)
        {
            return Operator == ConditionOperator.AND ?
                Conditions.TrueForAll(c => c.Evaluate(evt)) :
                Conditions.Any(c => c.Evaluate(evt));
        }
    }
}
