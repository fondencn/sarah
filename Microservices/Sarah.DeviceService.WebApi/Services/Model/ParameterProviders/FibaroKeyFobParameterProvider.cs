using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.DeviceService.Model.ParameterProviders
{
    internal class FibaroKeyFobParameterProvider : AbstractParameterProvider
    {
        private Dictionary<byte, string> InternalParameterNames { get; } = new Dictionary<byte, string>()
        {

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
