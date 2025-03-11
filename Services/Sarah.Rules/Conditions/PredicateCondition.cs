using Sarah.API.BusinessObjects;
using System;

namespace Sarah.Rules.Conditions
{
    public class PredicateCondition : RuleCondition
    {
        private Predicate<NetworkEvent> Condition { get; }


        public PredicateCondition(byte nodeId, Predicate<NetworkEvent> predicate) : base(nodeId)
        {
            this.Condition = predicate;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            return Condition.Invoke(evt);
        }
    }
}
