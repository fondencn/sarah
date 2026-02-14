using System.Collections.Generic;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class AeotecZstickParameterProvider : AbstractParameterProvider
    {
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {
            {81, "LED-Blinken bei USB-Verbindung (Manuell siehe Parameter 0)" },
            {0, @" pi@pi:~ $ echo -e -n ""\x01\x08\x00\xF2\x51\x01\x00\x05\x01\x51"" > /dev/serial/by-id/usb-0658_0200-if00" },


        };

        protected override IEnumerable<byte> GetKnownParameters() => InternalParameterNames.Keys;

        protected override string? GetParameterName(byte paramId)
        {
            InternalParameterNames.TryGetValue(paramId, out string? res);
            return res;
        }
    }
}
