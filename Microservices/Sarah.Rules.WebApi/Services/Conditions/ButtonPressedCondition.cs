using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class ButtonPressedCondition : RuleCondition
    {
        private readonly IDeviceService _devices;

        public byte? SceneId { get;  }



        public ButtonPressedCondition(byte buttonNodeId, IDeviceService devices) : this(buttonNodeId, null, devices)
        {
        }


        public ButtonPressedCondition(byte buttonNodeId, byte? sceneid, IDeviceService devices) : base(buttonNodeId)
        {
            this._devices = devices;
            this.SceneId = sceneid;
        }


        public override bool Evaluate(NetworkEvent evt)
        {
            bool isMatch = evt.SourceNodeId == this.TargetNodeId;
            if(this.SceneId.HasValue)
            {
                IWallController button = _devices.GetNetworkItem(evt.SourceNodeId) as IWallController;
                if(button != null)
                {
                    isMatch &= button.LastSceneId == this.SceneId.Value;
                }
            }
            return isMatch;
        }
    }
}
