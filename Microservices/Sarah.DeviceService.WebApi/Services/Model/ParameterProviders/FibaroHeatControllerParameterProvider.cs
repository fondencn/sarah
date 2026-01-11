using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class FibaroHeatControllerParameterProvider : AbstractParameterProvider
    {
        /* via https://github.com/OpenZWave/open-zwave/blob/master/config/fibaro/fgt001.xml */
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {1, @"This parameter determines duration of Override Schedule after turning the knob while normal schedule is active (set by Schedule CC)" },
            {2, @"This parameter allows to enable different additional functions of the device. 
                1) Enable open window detector
                2) Enable fast open window detector
                4) Increase receiver sensitivity (shortens battery life)
                8) Enable LED indications when controlling remotely
                16) Protect from setting Full ON and Full OFF mode by turning the knob manually"},
            {3, @"This parameter allows to check status of different additional functions (READONLY).
                1) Optional temperature sensor connected and operational
                2) Open window detected" }


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
