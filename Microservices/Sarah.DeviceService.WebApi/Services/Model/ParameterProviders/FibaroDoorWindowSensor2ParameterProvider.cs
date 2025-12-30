using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class FibaroDoorWindowSensor2ParameterProvider : AbstractParameterProvider
    {
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"Mit diesem Parameter kann eingestellt werden, in welchem Zustand sich die Tür / das Fenster befindet, wenn sich der Magnet in der Nähe des Sensors befindet.
Verfügbare Werte:
0 – closed when magnet near
1 – opened when magnet near
Standardwert: 0
Parametergröße: 1 [Byte]" },
            {2, @"Dieser Parameter definiert welche Ereignisse durch visuelle LED-Anzeige angezeigt werden. Das Deaktivieren von Ereignissen kann die Batterielebensdauer verlängern.
Verfügbare Werte:
1 – Anzeige der Änderung des Öffnungs- / Schließzustands
2 – Anzeige des Aufwachens (1 x Klick oder regelmäßig)
4 – Manipulationsanzeige
Standardwert: 6
Parametergröße: 1 [Byte]" },
            {50, @"
            Dieser Parameter legt fest, wie oft die Temperatur gemessen wird. Je kürzer die Zeit, desto häufiger wird die Temperatur gemessen, aber die Batterielebensdauer wird verkürzt.
Verfügbare Werte:
0 – Temperaturmessungen deaktiviert
5-32400 – Zeit in Sekunden
Standardwert: 300 (5 Minuten)
Parametergröße: 2 [Bytes]" },
        };

        protected override IEnumerable<byte> GetKnownParameters() => InternalParameterNames.Keys;

        protected override string GetParameterName(byte paramId)
        {
            string res;
            if (!InternalParameterNames.TryGetValue(paramId, out res))
            {
                res = null;
            }
            return res;
        }
    }
}
