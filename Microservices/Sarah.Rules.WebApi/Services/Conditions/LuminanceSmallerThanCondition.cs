using System.Linq;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class LuminanceSmallerThanCondition : RuleCondition
    {
        private readonly IDeviceService _devices;

        private int LuminanceLimit { get; set; }
        public LuminanceSmallerThanCondition(byte targetNodeId, int luminanceLimit, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
            this.LuminanceLimit = luminanceLimit;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            var sensor = _devices.Sensors.First(item => item.NodeID == this.TargetNodeId);
            if (sensor.Luminance == null)
            {
                return false;
            }
            else
            {
                return ((sensor.Luminance?.Value) ?? float.MaxValue) < this.LuminanceLimit;
            }
        }
    }
}
