using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    /// <summary>
    /// Evaluiert zu true wenn keine Person als anwesend markiert ist
    /// </summary>
    public class SomeOnePresentCondition : RuleCondition
    {
        private readonly IPersonService _persons;
        public SomeOnePresentCondition(IPersonService persons) : base(0)
        {
            this._persons = persons;
        }

        public override bool Evaluate(NetworkEvent evt) => _persons.IsSomeonePresent().Result;
    }
}
