using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class FibaroWalliParameterProvider : AbstractParameterProvider
    {
        /* via http://manuals-backend.z-wave.info/make.php?lang=DE&sku=FIBEFGWDSEU-221&cert=ZC10-19056499 */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"Letzter Status
Dieser Parameter legt fest, wie das Gerät bei einem Ausfall der Stromversorgung (z.B. Stromausfall) reagiert.
Grösse: 1 Byte, Voreingestellt: 1
0 - bleibt nach dem Wiederherstellen der Stromversorgung ausgeschaltet.
1 -	stellt den gespeicherten Zustand nach dem Wiederherstellen der Stromversorgung wieder her." },

            {2, @"Parameter 2: Überlastschutz Kanal 1
Diese Funktion ermöglicht es, das kontrollierte Gerät bei Überschreitung der definierten Leistung auszuschalten, das kontrollierte Gerät kann über eine Taste oder durch Senden eines Kommandos wieder eingeschaltet werden.
Grösse: 4 Byte, Voreingestellt: 0
0 - Deaktiviert
1 -	(1.0-3620.0W, 0,1 Schritte) Leitungsgrenzwert" },

            {3, @"Parameter 3: Überlastschutz Kanal 2
Diese Funktion ermöglicht es, das kontrollierte Gerät bei Überschreitung der definierten Leistung auszuschalten, das kontrollierte Gerät kann über eine Taste oder durch Senden eines Kommandos wieder eingeschaltet werden.
Grösse: 4 Byte, Voreingestellt: 0
0 - Deaktiviert
1 -	(1.0-3620.0W, 0,1 Schritte) Leitungsgrenzwert" },

            {10, @"Parameter 10: LED Leistungslimit
Dieser Parameter bestimmt die maximale Wirkleistung. Bei Überschreitung führt dies zu einem violetten LED-Frameflash. Die Funktion ist nur aktiv, wenn Parameter 11 auf 8 oder 9 eingestellt ist.
Grösse: 4 Byte, Voreingestellt: 36800
500-36800 - (50.0-3680.0W, Schritte 0.1W) Grenzwert" },

            {11, @"Parameter 11: LED Farbe im Eingeschalteten Zustand
Dieser Parameter definiert die LED-Farbe, wenn das Gerät eingeschaltet ist. Bei Einstellung auf 8 oder 9 ändert sich die LED-Frame-Farbe je nach gemessener Leistung und Parameter 10. Andere Farben sind fest eingestellt und nicht vom Stromverbrauch abhängig.
Grösse: 1 Byte, Voreingestellt: 1
0	LED Deaktiviert
1	Weiß
2	Rot
3	Grün
4	Blau
5	Gelb
6	Cyan
7	Magenta
8	Farbwechsel je nach gemessener Leistung stufenlos möglich
9	Farbveränderungen in Stufen in Abhängigkeit von der gemessenen Leistung" },

            {12, @"Parameter 12: LED Farbe im ausgeschalteten Zustand
Dieser Parameter definiert die LED-Farbe, wenn das Gerät ausgeschaltet ist.
Grösse: 1 Byte, Voreingestellt: 0
0	LED deaktiviert
1	Weiß
2	Rot
3	Grün
4	Blau
5	Gelb
6	Cyan
7	Magenta" },

            {13, @"Parameter 13: LED Helligkeit
Mit diesem Parameter kann die Helligkeit des LED-Rahmens eingestellt werden.
Grösse: 1 Byte, Voreingestellt: 100
0	LED Deaktiviert
1 - 100	Helligkeit in %
101	Helligkeit direkt proportional zum eingestellten Wert
102	Helligkeit umgekehrt proportional zum eingestellten Wert" },

            {20, @"Parameter 20: Tastenbedienung
Dieser Parameter legt fest, wie Gerätetasten die Kanäle steuern sollen.
Grösse: 1 Byte, Voreingestellt: 1
1	1. und 2. Taste schaltet die Last um.
2	Die erste Taste schaltet die Last EIN, die zweite Taste schaltet die Last AUS." },

            {24, @"Parameter 24: Ausrichtung der Tasten
Mit diesem Parameter kann die Funktion der Tasten umgekehrt werden.
Grösse: 1 Byte, Voreingestellt: 0
0	Standard (1. Taste steuert den 1. Kanal, 2. Taste steuert den 2. Kanal)
1	vertauscht (1. Taste steuert 2. Kanal, 2. Taste steuert 1. Kanal)" },

            {25, @"Parameter 25: Ausrichtung der Ausgänge
Dieser Parameter ermöglicht es, den Betrieb von Q1 und Q2 umzukehren, ohne die Verdrahtung zu ändern (z.B. bei ungültiger Verbindung). Eine Änderung der Ausrichtung schaltet beide Ausgänge aus.
Grösse: 1 Byte, Voreingestellt: 0
0	Standard (Q1 - 1. Kanal, Q2 - 2. Kanal)
1	vertauscht (Q1 - 2. Kanal, Q2 - 1. Kanal)" },

            /* UND VIELE MEHR!!!! */

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
