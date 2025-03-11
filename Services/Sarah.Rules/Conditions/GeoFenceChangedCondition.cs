using Sarah.API.BusinessObjects;

namespace Sarah.Rules.Conditions
{
    internal class GeoFenceChangedCondition : RuleCondition
    {
        public GeoFenceChangedCondition() : base(0)
        {
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            return evt is PersonGeoFenceEvent;
        }
    }
}
