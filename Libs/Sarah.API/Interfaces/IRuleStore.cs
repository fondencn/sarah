using Sarah.API.BusinessObjects;
using System;
using System.Collections.Generic;

namespace Sarah.API.Interfaces
{
    /// <summary>
    /// Eine Datenquelle für Regeln des Regelspeichers
    /// </summary>
    public interface IRuleStore
    {
        /// <summary>
        /// Die Regeln in diesem Regelspeicher
        /// </summary>
        IReadOnlyCollection<Rule> Rules { get; }

        /// <summary>
        /// Wird ausgelöst, wenn sich eine Regel oder die gesamte Regelauflistung ändert
        /// </summary>
        event EventHandler Changed;
    }
}
