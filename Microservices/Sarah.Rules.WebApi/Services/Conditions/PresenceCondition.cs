using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using System.Linq;

namespace Sarah.Rules.Conditions
{
    public class PresenceCondition : RuleCondition
    {
        private readonly IDeviceService _devices;
        public bool Value { get; }

        public PresenceCondition(byte targetNodeId, bool targetValue, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
            this.Value = targetValue;
        }

        public override bool Evaluate(NetworkEvent evt ) 
        {
            try
            {
                IMultiSensor sensor = _devices.Sensors.First(item => item.NodeID == this.TargetNodeId);
                if (sensor.Presence != null)
                {
                    return (((sensor.Presence?.Value) ?? 0) > 0) == this.Value;
                }
                else
                {
                    return false;
                }
            } 
            catch
            {
                return false;
            }
        }
    }
}
