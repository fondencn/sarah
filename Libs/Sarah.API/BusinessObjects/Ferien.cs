using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.BusinessObjects
{

    /// <summary>
    /// Ein einzelner Eintrag in der Ferienliste
    /// </summary>
    public class Ferien
    {
        /// <summary>
        /// Startdatum der Ferien
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// Enddatum der Ferien
        /// </summary>
        public DateTime Ende { get; set; }
        /// <summary>
        /// Name der Ferien
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Name} von {Start} bis {Ende}";
        }
    }
}
