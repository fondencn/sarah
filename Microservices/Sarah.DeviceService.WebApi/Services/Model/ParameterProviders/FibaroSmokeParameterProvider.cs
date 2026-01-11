using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class FibaroSmokeParameterProvider : AbstractParameterProvider
    {
        /* via https://manuals.fibaro.com/de/document-category/smoke-sensor-de/ */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"Empfindlichkeit des Smoke Sensors
Es gibt 3 Empfindlichkeitsstufen für Rauch. Stufe 1 bedeutet die höchste Empfindlichkeit. 
Durch Erhöhen des Parameterwerts wird die Empfindlichkeit gegenüber Rauch verringert.
Grösse: 1 Byte, Voreingestellt: 2
1 – HOHE Empfindlichkeit (HIGH)
2 – MITTLERE Empfindlichkeit (MIDDLE)
3 – NIEDRIGE Empfindlichkeit (LOW)" },

            {2, @"Status der Z-Wave-Benachrichtigungen
Mit diesem Parameter können Sie Benachrichtigungen über Übertemperatur und / oder Gehäuseöffnung aktivieren, 
die an den Hauptnetzwerkcontroller gesendet werden.
Grösse: 1 Byte, Voreingestellt: 0
0 – Alle Benachrichtigungen deaktiviert
1 – Gehäuseöffnungsbenachrichtigung aktiviert
2 – Benachrichtigung über Überschreitung des Temperaturschwellenwerts aktiviert
3 - Alle Benachrichtigungen aktiviert" },

            {3, @"Status der visuellen Indikatorbenachrichtigungen
Dieser Parameter ermöglicht die Aktivierung visueller Anzeigen, gilt jedoch nicht 
für Hauptalarme wie FEUERSTÖRUNG und BATTERIE-NIEDRIG-ALARM.
Grösse: 1 Byte, Voreingestellt: 0
0 – Alle Benachrichtigungen deaktiviert
1 – Gehäuseöffnungsbenachrichtigung aktiviert
2 – Benachrichtigung über Überschreitung des Temperaturschwellenwerts aktiviert
4 – Benachrichtigung über fehlendes Z-Wave-Netzwerksignal" },

            {4, @"Tonbenachrichtigungsstatus
Dieser Parameter ermöglicht die Aktivierung von Tonsignalen, gilt jedoch nicht für Hauptalarme wie 
FEUERSTÖRUNG (FIRE TROUBLE) und BATTERIE-NIEDRIG-ALARM (LOW BATTERY ALARM).
Grösse: 1 Byte, Voreingestellt: 0
0 – Alle Benachrichtigungen deaktiviert
1 – Gehäuseöffnungsbenachrichtigung aktiviert
2 – Benachrichtigung über Überschreitung des Temperaturschwellenwerts aktiviert
4 – kein Z-Wave-Netzwerksignal Benachrichtigung aktiv" },

            {10, @"Konfiguration von Kontrolrahmen in der BASIC-Kommandoklasse
Dieser Parameter definiert, welche Frames in der 2. Assoziationsgruppe (FIRE ALARM) gesendet werden. 
Die Werte der Rahmen BASIC ON und BASIC OFF können wie in den weiteren Parametern beschrieben definiert werden.
Grösse: 1 Byte, Voreingestellt: 0
0 – BASIC ON & BASIC OFF aktiviert
1 – nur BASIC ON aktiviert
2 – nur BASIC OFF aktiviert" },

            {11, @"BASIC ON-Rahmenwert
Der BASIC ON-Frame wird im Falle einer Raucherkennung und einer FIRE ALARM-Auslösung gesendet. 
Sein Wert wird durch diesen Parameter definiert.
Grösse: 1 Byte, Voreingestellt: 255
Verfügbare Werte: 0-99, 255
0 – Gerät ausschalten
1-99 – stelle das Gerät auf 1-99%
255 – den letzten Status wiederherstellen" },

            {12, @"BASIC OFF-Rahmenwert
Der BASIC OFF-Rahmen wird im Falle der Löschung des FEUERALARMS gesendet. Sein Wert wird durch diesen Parameter definiert.
Grösse: 1 Byte, Voreingestellt: 0
Verfügbare Werte: 0-99, 255
0 – Gerät ausschalten
1-99 – stelle das Gerät auf 1-99%
255 – den letzten Status wiederherstellen" },

            {13, @"Alarmübertragung (Broadcast-Modus)
Ein anderer Wert als 0 bedeutet, dass Alarme im Broadcast-Modus gesendet werden, d. H. an alle Geräte in der Reichweite eines FIBARO Smoke Sensors.
Grösse: 1 Byte, Voreingestellt: 0
Verfügbare Werte: 0-99, 255
0 – Broadcast-Modus inaktiv
1 – FIRE ALARM Broadcast (für 2. und 4. Assoziationsgruppe) aktiv; Broadcast über das Öffnen des Gehäuses (für die 3. und 5. Assoziationsgruppe) inaktiv
2 – FIRE ALARM Broadcast (für 2. und 4. Assoziationsgruppe) inaktiv; Broadcast über das Öffnen des Gehäuses (für die 3. und 5. Assoziationsgruppe) aktiv
3 – FIRE ALARM Broadcast (für 2. und 4. Assoziationsgruppe) aktiv; Broadcast über das Öffnen des Gehäuses (für die 3. und 5. Assoziationsgruppe) aktiv" },

            {20, @"Temperaturberichtsintervall
Zeitintervall zwischen aufeinanderfolgenden Temperaturberichten.
Ein Temperaturbericht wird gesendet, wenn sich der neue Temperaturwert von dem zuvor gemeldeten unterscheidet 
– entsprechend der eingestellten Hysterese (Parameter 21). 
Temperaturberichte können auch als Ergebnis von Geräteabfragen gesendet werden.
Grösse: 2 Byte, Voreingestellt: 1 (10 Sekunden)
Verfügbare Werte:0, 1-8640 (multipliziert mit 10 Sekunden) [10s-24h] 
0 – Temperaturberichte inaktiv" },

            {21, @"Hysterese des Temperaturberichts
Ein Temperaturbericht wird nur gesendet, wenn sich der Temperaturwert von dem in diesem Parameter definierten vorherigen Wert unterscheidet (Hysterese). 
Temperaturberichte können auch als Ergebnis von Geräteabfragen gesendet werden.
Grösse: 1 Byte, Voreingestellt: 10
Verfügbare Werte:
1-100 (in 0.1°C Schritten)
1-100 – (multipliziert mit 0,1) [0.1°C – 10°C]" },

            {30, @"Temperaturschwelle
Temperaturwert, der vom eingebauten Temperatursensor gemessen wird, 
über dem die Übertemperaturmeldung gesendet wird 
(visuelle Anzeige / Ton / Z-Wave-Bericht).
Grösse: 1 Byte, Voreingestellt: 55
Verfügbare Werte: 1-100
1-100 – (1°C – 100°C)" },

            {31, @"Signalisierungsintervall für Übertemperatur
Zeitintervall der Signalisierung (visuelle Anzeige / Ton) des Übertemperaturniveaus.
Grösse: 2 Byte, Voreingestellt: 1
1-8640 (multipliziert mit 10 Sekunden) [10s-24h] 1-8640 – [10s-24h] Standardwert: 1 (10 Sekunden)" },


            {32, @"Intervall für keine Z-Wave-Netzwerksignalanzeige
Zeitintervall für keine Z-Wave-Netzwerksignalanzeige (visuelle Anzeige / Ton)
Grösse: 2 Byte, Voreingestellt: 180
1-8640 (multipliziert mit 10 Sekunden) 
[10s-24h] 
1-8640 – [10s-24h] 
Standardwert: 180 (30 Minuten)" },
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
