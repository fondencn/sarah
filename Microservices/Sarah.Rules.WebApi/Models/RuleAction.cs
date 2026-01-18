using Sarah.API.BusinessObjects;

namespace Sarah.Rules.Models
{
    /// <summary>
    /// Basisklasse für Aktionen, die beim Zutreffen einer Regel ausgeführt wird
    /// </summary>
    public abstract class RuleAction
    {
        /// <summary>
        /// Wird aufgerufen, wenn die zugeordnete Regel zutrifft
        /// </summary>
        public abstract void Execute(NetworkEvent sourceEvent);
    }

}
