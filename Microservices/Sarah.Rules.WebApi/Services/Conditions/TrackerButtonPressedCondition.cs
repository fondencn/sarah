using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class TrackerButtonPressedCondition : RuleCondition
    {
        public byte? SceneId { get;  }
        private readonly IDeviceService _devices;



        public TrackerButtonPressedCondition(byte buttonNodeId, IDeviceService devices) : this(buttonNodeId, null, devices)
        {
            this._devices = devices;
        }


        public TrackerButtonPressedCondition(byte buttonNodeId, byte? sceneid, IDeviceService devices) : base(buttonNodeId)
        {
            this.SceneId = sceneid;
            this._devices = devices;
        }


        public override bool Evaluate(NetworkEvent evt)
        {
            bool isMatch = evt.SourceNodeId == this.TargetNodeId;
            IGPSTracker tracker = _devices.GetNetworkItem(evt.SourceNodeId) as IGPSTracker;
            if(tracker != null)
            {
                isMatch &= tracker.IsButtonPressed.Value != 0f; 
            }
            return isMatch;
        }
    }
}
