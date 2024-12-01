using Sarah.API.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.Interfaces
{
    public interface IFerienInfoProvider
    {

        /// <summary>
        /// Die bekannten Schulferien als vereinheitlichte Liste
        /// </summary>
        public IReadOnlyCollection<Ferien> Ferien { get; }


        /// <summary>
        /// Gibt das aktuelle Ferienelemente (für heute) zurück oder NULL, falls keine Ferien sind.
        /// </summary>
        public Ferien AktuelleFerien { get; }
    }
}
