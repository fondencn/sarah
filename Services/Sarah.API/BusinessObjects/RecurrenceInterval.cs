using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Wiederholungsintervalle für Termine
    /// </summary>
    public enum RecurrenceInterval
    {
        /// <summary>
        /// Täglich
        /// </summary>
        Täglich = 0,
        /// <summary>
        /// Wöchentliche
        /// </summary>
        Wöchentlich = 1,
        /// <summary>
        /// Monatlich
        /// </summary>
        Monatlich = 2,
        /// <summary>
        /// Jährlich
        /// </summary>
        Jährlich = 3
    }
}
