using Sarah.API.BusinessObjects;
using System;

namespace Sarah.Rules.Conditions
{
    public class PersonPresenceChangedCondition : RuleCondition
    {
        private string PersonName { get;  }
        private bool IsPresent { get; }
        public PersonPresenceChangedCondition(string personName, bool isPresent) : base(0)
        {
            this.PersonName = personName;
            this.IsPresent = isPresent;
        }
        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is PersonAvailabilityEvent personEvent)
            {
                if (String.Equals(personEvent.PersonName, this.PersonName, StringComparison.OrdinalIgnoreCase))
                {
                    return ((PersonAvailabilityEvent)evt).IsAvailable == IsPresent;
                } 
                else
                {
                    return false;
                }
            } 
            else
            {
                return false;
            }
        }
    }
}
