using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class AeotecLedBulbParameterProvider : AbstractParameterProvider
    {
        /* via
         * https://manual.zwave.eu/backend/make.php?lang=de&sku=AEOEZWA002
         *
         * Page starting at "Configuration Parameters"  */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
//            {112, @"Set the Dimmer mode. Size: 1 Byte, Default Value: 0 
//0 - Parabolic curve mode
//1 - Index curve mode
//2 - (Parabolic + Index)/2 mode
//3 - Linear mode" },

            {3, @"Parameter 3 Änderungsgeschwindigkeit
Stellen Sie die Änderungsgeschwindigkeit auf die nächste Farbe im benutzerdefinierten Modus ein.
Grösse: 1 Byte, Voreingestellt: 50
5 - 8640000	Änderungsgeschwindigkeit in X = 10 ms"},

            {16, @"Parameter 16 Dimmgeschwindigkeit
Dimmgeschwindigkeit wenn Multilevel Swicth CC V1 benutzt wird
Grösse: 1 Byte, Voreingestellt: 20.
0 - 100	Dimmgeschwindigkeit X = 100ms"},

//            {20, @"Set the bulbs state after it is re-powered on. Size: 1 Byte, Default Value: 1.
//1 - Always On.
//2 - Always Off.
//0 - The last state before re-power on."},

            {80, @"Parameter 80 Bericht in Gruppe 1
Aktiviert den Bericht, welcher an die Grupp 1 gesendet werden soll.
Grösse: 1 Byte, Voreingestellt: 1
0 - Kein Bericht
1 - Basic Set (EIN/AUS) Bricht, wenn sich der Status der Leuchte ändert."},
        };

        protected override IEnumerable<byte> GetKnownParameters() => InternalParameterNames.Keys;

        protected override string? GetParameterName(byte paramId)
        {
            InternalParameterNames.TryGetValue(paramId, out string? res);
            return res;
        }
    }
}
