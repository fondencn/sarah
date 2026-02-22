using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class PoppWallControllerParameterProvider : AbstractParameterProvider
    {
        /* via http://manuals-backend.z-wave.info/make.php?lang=DE&sku=FIBEFGWDSEU-221&cert=ZC10-19056499 */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"Steuerungsmöglichkeiten der Tasten 1 und 3
Mit diesem Parameter wird festgelegt, welche Tastengruppen die Tasten 1 und 3 steuern sollen.
Grösse: 1 Byte, Voreingestellt: 1
0 - Eigene Steuergruppe pro Taste
1 - Gemeinsames Steuern der Tastengruppe A, kein Doppelklick
2 -	Gemeinsames Steuern der Tastengruppe A, jeweils Doppelklicks für Tastengruppe C" },

            {2, @"Steuerungsmöglichkeiten der Tasten 2 und 4
Mit diesem Parameter wird festgelegt, welche Tastengruppen die Tasten 2 und 4 steuern sollen.
Grösse: 1 Byte, Voreingestellt: 1
0 - Eigene Steuergruppe pro Taste
1 - Gemeinsames Steuern der Tastengruppe B, kein Doppelklick
2 -	Gemeinsames Steuern der Tastengruppe B, jeweils Doppelklicks für Tastengruppe D" },

            {11, @"Kommando für Geräte der Tastengruppe A
Mit diesem Parameter wird festgelegt, welches Z-Wave Kommando die Geräte der Tastengruppe A erhalten
Grösse: 1 Byte, Voreingestellt: 8
0 - Deaktiviert
1 - Ein/Aus-Schalten und Dimmen (BASIC Set und Switch Multilevel)
2 - Nur Ein/Aus-Schalten (BASIC Set)
3 - Alle Geräte in Umgebung (Switch All)
4 - Komplexe Szenen (mehrere Szenen pro Taste)
5 - Einfache Szenen (eine Szene pro Taste)
6 - UNDEFINIERT
7 - Türschloss (Door Lock)
8 -	Zentrale Szenensteuerung im Gateway (Central Scene)" },

            {12, @"Kommando für Geräte der Tastengruppe B
Mit diesem Parameter wird festgelegt, welches Z-Wave Kommando die Geräte der Tastengruppe B erhalten
Grösse: 1 Byte, Voreingestellt: 8
0 - Deaktiviert
1 - Ein/Aus-Schalten und Dimmen (BASIC Set und Switch Multilevel)
2 - Nur Ein/Aus-Schalten (BASIC Set)
3 - Alle Geräte in Umgebung (Switch All)
4 - Komplexe Szenen (mehrere Szenen pro Taste)
5 - Einfache Szenen (eine Szene pro Taste)
6 - UNDEFINIERT
7 - Türschloss (Door Lock)
8 -	Zentrale Szenensteuerung im Gateway (Central Scene)" },


            {13, @"Kommando für Geräte der Tastengruppe C
Mit diesem Parameter wird festgelegt, welches Z-Wave Kommando die Geräte der Tastengruppe C erhalten
Grösse: 1 Byte, Voreingestellt: 8
0 - Deaktiviert
1 - Ein/Aus-Schalten und Dimmen (BASIC Set und Switch Multilevel)
2 - Nur Ein/Aus-Schalten (BASIC Set)
3 - Alle Geräte in Umgebung (Switch All)
4 - Komplexe Szenen (mehrere Szenen pro Taste)
5 - Einfache Szenen (eine Szene pro Taste)
6 - UNDEFINIERT
7 - Türschloss (Door Lock)
8 -	Zentrale Szenensteuerung im Gateway (Central Scene)" },



            {14, @"Kommando für Geräte der Tastengruppe D
Mit diesem Parameter wird festgelegt, welches Z-Wave Kommando die Geräte der Tastengruppe D erhalten
Grösse: 1 Byte, Voreingestellt: 8
0 - Deaktiviert
1 - Ein/Aus-Schalten und Dimmen (BASIC Set und Switch Multilevel)
2 - Nur Ein/Aus-Schalten (BASIC Set)
3 - Alle Geräte in Umgebung (Switch All)
4 - Komplexe Szenen (mehrere Szenen pro Taste)
5 - Einfache Szenen (eine Szene pro Taste)
6 - UNDEFINIERT
7 - Türschloss (Door Lock)
8 -	Zentrale Szenensteuerung im Gateway (Central Scene)" },


            {21, @"Wie werden Geräte in der Umgebung geschaltet (SwitchAll)
Dieser Parameter definiert, welche Befehle als Broadcast an alle Geräte in der Umgegend gesendet werden sollen.
Grösse: 1 Byte, Voreingestellt: 1
1 - Nur Ausschalten
2 - Nur Einschalten
255 -	Einschalten und Ausschalten" },


            {22, @"Invertiere Tastenbedeutung
Dieser Parameter ermöglicht, die Kommandos einer Tastengruppe zu invertieren.
Grösse: 1 Byte, Voreingestellt: 0
0 - Ja
1 -	Nein" },


            {25, @"Blockierung des Aufweckens trotz gesetztem Aufweckinterval
Ermöglicht einen zusätzlichen Schutz gegen falsch konfigurierende Gateways
Grösse: 1 Byte, Voreingestellt: 1
0 - Blockiert
1 -	Erlaubt" },

            {30, @"Unaufgefordert bei Aufwecken einen Batteriereport senden
Grösse: 1 Byte, Voreingestellt: 0
0 - Nein
1 - Zum Gateway
2 -	Als Broadcast" },

        };

        protected override IEnumerable<byte> GetKnownParameters() => InternalParameterNames.Keys;

        protected override string? GetParameterName(byte paramId)
        {
            InternalParameterNames.TryGetValue(paramId, out string? res);
            return res;
        }
    }
}
