using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class FibaroRGBWController2ParameterProvider : AbstractParameterProvider
    {
        /* via https://manuals.fibaro.com/rgbw-2/
         * Es gibt noch viel mehr Parameter, die wir aber momentan nicht benötigen */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"This parameter determines how the device will react in the event of power supply failure (e.g. power outage or taking out from the electrical outlet).
After the power supply is back on, the device can be restored to previous state or remain switched off. The sequence is not remembered after the power returns. After power failure, the last color set before the sequence will be restored.
Parameter size: 1B
Default value: 0
Available values:
0 – device remains switched off
1 – device restores the state from before the power failure" },
            {150, @"Color Mode (1=RGBW,2=HSV)" },
            {152, @"Dieser Parameter bestimmt die Zeit, die zum Ändern des Status zwischen aktuellen und Zielwerten bei der Steuerung über das Z-Wave-Netzwerk benötigt wird.
Parametergröße: 2B
Standardwert: 3 (3 Sek.)
Verfügbare Werte:
0 – sofort
1-127 (1 Sek.-127 Sek. in 1 Sek.-Schritten)
128-254 (1 Min.-127 Min. in 1 Min.-Schritten)" },
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
