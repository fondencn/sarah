using Sarah.API.BusinessObjects;
using System.Collections.Generic;
using System.Linq;

namespace Sarah.Rules.Actions
{
    /// <summary>
    /// Kombinierte Action. Führt alle übergebenen Actions nacheinander aus.
    /// </summary>
    public class CombinedAction : RuleAction
    {
        /// <summary>
        /// Die Actions, die hier gemeinsam ausgeführt werden
        /// </summary>
        public IReadOnlyCollection<RuleAction> Actions { get; }

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="actions">Die Actions, die hier gemeinsam ausgeführt werden</param>
        public CombinedAction(params RuleAction[] actions)
        {
            this.Actions = actions.ToList().AsReadOnly();
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            foreach (RuleAction action in this.Actions)
            {
                action.Execute(sourceEvent);
            }
        }
    }
}
