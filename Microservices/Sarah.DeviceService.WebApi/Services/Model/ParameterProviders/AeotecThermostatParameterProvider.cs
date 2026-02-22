using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class AeotecThermostatParameterProvider : AbstractParameterProvider
    {
        /* via https://aeotec.freshdesk.com/support/solutions/articles/6000224131
         * Alle Paremter eingebaut */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"Inverts the LCD orientation.
Size: 1 Byte, Default Value: 0
Available values:
0 – Normal orientation
1 – LCD content inverted" },
            {2, @"Configures the timeout of the LCD. 
Size: 1 Byte, Default Value: 0
Available values:
0 – LCD always on
5-30 – 	LCD timeout in seconds" },
            {3, @"Enables or disables the LCD-Backlight. 
Size: 1 Byte, Default Value: 1
Available values:
0 – Backlight disabled
1 – Backlight enabled" },
            {4, @"Enables or disables unsolicited battery reporting once a day. 
Size: 1 Byte, Default Value: 1
Available values:
0 – Battery reporting disabled
1 –Battery reporting enabled" },
            { 5, @"Reports the measured room temperature on change. 
Size: 1 Byte, Default Value: 5
Available values:
0 – Reporting disabled
1-50 – Reporting Delta in 1/10 Celcius" },
            {6, @"Reports the valve percentage on change. 
Size: 1 Byte, Default Value: 0
Available values:
0 – Reporting disabled
1-100 –Reporting Delta in percent" },
            {7, @"Configures the sensitivity of the window open detection. 
Size: 1 Byte, Default Value: 2
Available values:
0 – 	Detection disabled
1-3 – Sensitivity level low-medium-high" },
            {8, @"Configures an offset for the measured temperature. Set the offset to -128 (0x80) if measured temperature is provided externally. 
Size: 1 Byte, Default Value: 0
Available values:
0-50 – Offset in 1/10 Celcius (0°C - 5°C)
128	 – Temperature is supplied externally
206-255 - Offset in 1/10 Celcius (-5°C - 0,1°C)" },
        };

        protected override IEnumerable<byte> GetKnownParameters() => InternalParameterNames.Keys;

        protected override string? GetParameterName(byte paramId)
        {
            InternalParameterNames.TryGetValue(paramId, out string? res);
            return res;
        }
    }
}
