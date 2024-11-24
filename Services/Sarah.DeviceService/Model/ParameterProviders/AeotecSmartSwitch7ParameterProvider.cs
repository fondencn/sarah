using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class AeotecSmartSwitch7ParameterProvider : AbstractParameterProvider
    {
        /* via https://manual.zwave.eu/backend/make.php?lang=de&sku=AEOEZW175/ */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {4, @"Definieren Sie eine Schwellenleistung und schalten Sie den Schalter automatisch aus, wenn die angeschlossene Last die maximal zulässige Leistung überschreitet, unabhängig davon, ob sie immer eingeschaltet ist oder nicht. Der Überlastschutz ist aktiv, wenn die Lastleistung die Einstellung überschreitet und länger als 30 Sekunden anhält. Wenn diese Option aktiv ist, blinkt die Kontrollleuchte rot und das Produkt sendet einen Benachrichtigungsbericht (Überlast erkannt) und deaktiviert die Funktion, die manuell oder RF den Schaltzustand steuert, bis der Benutzer den Schutzstatus auf ungeschützt durch das Gateway oder die Steuerung setzt. Auch wenn das Gerät ausgeschaltet ist, bleibt der Schutzstatus erhalten.
Grösse: 2 Byte, Voreingestellt: 2415
Available values:
0 – Deaktiviert den Überlastschutz (nicht empfohlen)
1-2415 – Grenzwert in Watt" },
            {8, @"Wenn ein durch die Alarmeinstellungen aktivierter Alarm empfangen wird, blinkt die Anzeigeleuchte basierend auf Parameter 0x12 (18) weiter. Es wird dem Benutzer untersagt, den Schaltzustand manuell oder per Funk zu steuern, bis der Alarm deaktiviert wird.
Grösse: 1 Byte, Voreingestellt: 0
Available values:
0 – Deaktivieren, keine Reaktion auf Alarmeinstellungen
1 – Schaltet ein
2 – Schaltet aus
3 – Der Schalter schaltet sich in 5 Sekunden ein und dann in einem Zyklus in 5 Sekunden aus, bis der Benutzer den Alarm manuell deaktiviert." },
            {9, @"Bestimmen Sie, ob in Switch Alarme aktiviert sind und welcher Switch auf welchen Alarm reagieren soll. Das Format des Parameters ist Bitfeld (Checkboxen). Der Parameter MUSS wie ein Bitfeld behandelt werden, in dem jedes einzelne Bit gesetzt oder zurückgesetzt werden kann. Ein grafisches Konfigurationstool sollte diesen Parameter als eine Reihe von Kontrollkästchen darstellen. Weitere Informationen über die unterstützte Benachrichtigungsart und das Benachrichtigungsereignis finden Sie im Handbuch.
Grösse: 2 Byte, Voreingestellt: 0
Available values:
1 – Zugriffskontrolle Auslösestatus
256 – Rauchalarm
512 – CO Alarm
1024 – CO2 Alarm
2048 – Temperaturalarm
4096 – Wasseralarm
8192 – Zugriffssteuerung
16384 – Home Security"
            },
            {10, @"Deaktivieren des Alarms - Hinweis: Beim Ausschalten wird auch die Alarmreaktion ohne Einschränkung deaktiviert.
Grösse: 2 Byte, Voreingestellt: 0
Available values:
0 – Kann durch 3-faches Antippen der Aktionstaste innerhalb von 1 Sekunde deaktiviert werden.
1 – Kann deaktiviert werden, wenn ein Zustand Leerlauf entsprechend dem Alarm empfangen wird. Hinweis: Wenn die Zugangskontrolle aktiviert ist, kann sie auch deaktiviert werden, wenn sie den Umkehrstatus von Fenster/Tür empfängt.
10-255 – Stellt die Dauer des Alarms in Sekunden ein (d.h. der Kunde setzt diese Einstellung auf 50, der Alarmzustand des Schalters wird nach 50 Sekunden deaktiviert)."
            },
            {18, @"LED Blinkfrequenz
Wenn der Aktivierungsalarm empfangen wird, blinkt er entsprechend der durch diesen Parameter konfigurierten Blinkfrequenz, bis der Alarm deaktiviert wird. Helligkeitsgrad und Farbe basieren auf dem aktuellen Indikator. Wenn der Wert für Helligkeitsgrad und Farbe 0 ist, blinkt er basierend auf der letzten sichtbaren Farbe.
Grösse: 1 Byte, Voreingestellt: 2
Available values:
1-9 – Stellen Sie die Anzahl der Blinksignale pro Sekunde ein."
            },
            {19, @"Start- oder Stopp-LED blinkt (nur Schreiben)
Der Parameter kann verwendet werden, um die Wirkung des Blinkens der LED zu testen.
Grösse: 2 Byte, Voreingestellt: 0
Available values:
0 – Stoppt das Blinken
1-255 –	Stellen Sie die Dauer ein und starten Sie den Blinkprozess. * Hiermit wird der Zeitrahmen für das Blinken in Sekunden eingestellt. * Sobald die Dauer abgelaufen ist, hört das Blinken auf."
            },
            {20, @"Aktion bei Stromausfall.
Aktion bei Stromausfall.
Grösse: 1 Byte, Voreingestellt: 0
Available values:
0 – Letzter Status
1 – Schalter ist ein
2 – Schalter ist aus"
            },
            {80, @"Statusänderung Gruppe 1
Konfigurieren Sie, welcher Befehl über die Lifeline gesendet wird, wenn sich der Schaltzustand geändert hat.
Grösse: 1 Byte, Voreingestellt: 2
Available values:
0 – Keine
1 – Basic Report
2 – Binary Switch Report"
            },
            {81, @"Einstellung des Lastindikator-Modus.
Hinweis: Die Konfiguration von Helligkeit und Farbe der Anzeigeleuchte in unterschiedlichen Zeiten/Status der verschiedenen Modi wird im aktuellen Einstellmodus gespeichert. Weitere Informationen finden Sie im Handbuch.
Grösse: 1 Byte, Voreingestellt: 2
Available values:
0 – Deakiviert
1 – Nachtlicht Modus
2 – Ein/Aus Modus"
            },
            {82, @"Konfigurieren Sie die Aktivierungs- und Deaktivierungszeit des Nachtlichtmodus.
Wenn Sie den Nachtlichtmodus so einstellen möchten, dass er nachts um 19:00 Uhr aktiviert und morgens um 07:30 Uhr deaktiviert wird, müssen Sie nur konfigurieren: Wert1=0x13, Wert2=0x00, Wert3=0x07, Wert4=0x1E.
Grösse: 4 Byte, Voreingestellt: 301991936
Available values:
0-2147483647 – Wert1=aktiviert Stunde, Wert2=aktiviert Minute, Wert3=deaktiviert Stunde, Wert4=deaktiviert Minute"
            },
            {91, @"Autobericht Leistung (W)
Schwellenwertleistung (W) zum Einleiten eines automatischen Berichts.
Grösse: 2 Byte, Voreingestellt: 0
Available values:
0 – Deakiviert
1-2300 – 1-2300W"
            },
            {92, @"Autobericht Gesamtleistung (kWh)
Schwellenwertleistung (kWh) zum Einleiten eines automatischen Berichts.
Grösse: 2 Byte, Voreingestellt: 0
Available values:
0 – Deakiviert
1-10000 – 1-10000kWh"
            },
            {93, @"Autobericht Strom (A)
Schwellenwertstrom (A) zum Einleiten eines automatischen Berichts.
Grösse: 1 Byte, Voreingestellt: 0
Available values:
0 – Deakiviert
1-100 – 0.1-10A. Unit is 0.1A."
            },
            {101, @"Konfigurieren Sie, welcher Zählerstand periodisch über Lifeline gemeldet wird.
Das Format des Parameters ist Bitfeld (Checkboxen). Der Parameter MUSS wie ein Bitfeld behandelt werden, in dem jedes einzelne Bit gesetzt oder zurückgesetzt werden kann. Ein grafisches Konfigurationstool sollte diesen Parameter als eine Reihe von Kontrollkästchen darstellen. Hinweis: Die Sendefrequenz bezieht sich auf den Konfigurationsparameter 0x6F (111).
Grösse: 4 Byte, Voreingestellt: 15
Available values:
1 – Leistungsaufnahme (W)
2 – Leistung (kWh)
4 – Spannung (V)
8 – Strom (A)"
            },
            {111, @"Konfigurieren Sie die Sendefrequenz des Zählerberichts.
Konfigurieren Sie die Sendefrequenz des Zählerberichts.
Grösse: 4 Byte, Voreingestellt: 600
Available values:
0 - deaktiviert
600-2592000 – 600-2592000s. (10Minuten-30Tage)"
            },
            {255, @"Werkseinstellung (nur Schreiben)
Werkseinstellung
Grösse: 1 Byte, Voreingestellt: 0
Available values:
1431655765 – Werkseinstellung: Stellen Sie das Produkt auf die Werkseinstellungen zurück und entfernen Sie es aus dem Netzwerk.
0 – Initialisierung: Initialisieren Sie alle Konfigurationsparameter auf Standardwerte."
            },
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
