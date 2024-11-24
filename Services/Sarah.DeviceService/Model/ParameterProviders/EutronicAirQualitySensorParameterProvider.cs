using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class EutronicAirQualitySensorParameterProvider : AbstractParameterProvider
    {
        /* via https://manual.zwave.eu/backend/make.php?lang=de&sku=AEOEZW175/ */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"Temperatur on Change Reporting
0x00 Temperatur nicht bei Änderung melden.
0x01 - 0x32 Bei Temperaturdifferenz von 0,1°C - 5,0°C Ist-Temperatur übertragen 
default 0x05" },
            {2, @"Feuchtigkeit on Change Reporting
0x00 Feuchtigkeit nicht bei Änderungen melden. 
0x01-0x0A 1% Schritten 
default: 0x05" },
            {3, @"Einheit Temperatur
0x00 Temperatur in Grad Celcius
0x01 Temperatur in Grad Fahrenheit
default: 0x00" },
            {4, @"Auflösung Temperatur
0x00 keine Nachkommastelle
0x01 eine Nachkommastelle
0x02 zwei Nachkommastellen
default: 0x01" },
            {5, @"Auflösung Feuchte
0x00 keine Nachkommastelle
0x01 eine Nachkommastelle
0x02 zwei Nachkommastellen
default: 0x00" },
            {6, @"VOC-on Change Reporting
0x00 VOC-Gehalt (ppm) nicht bei Änderung melden. 
0x01 – 0x0A Bei ppb-Differenz von 
100ppb –1000ppb VOC-Gehalt übertragen 
default: 0x05" },
            {7, @"CO²eq on Change Reporting
0x00 CO²eq -Gehalt (ppm) nicht bei Änderung melden. 
0x01 – 0x0A Bei ppm-Differenz von 
100ppm –1000ppm CO�eq -Gehalt übertragen 
default: 0x05" },
            {8, @"Luftgüte per LED signalisieren
0x00 Luftgüte nicht per LED signalisieren
0x01 Luftgüte per LED signalisieren. (Gut oder schlecht) 
default: 0x01" },
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
