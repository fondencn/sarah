using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.Model.Animations;
using System.Linq;

namespace Sarah.Rules.Actions
{
    public class BlinkAction : RuleAction
    {
        private readonly IDeviceService _devices;

        public BlinkAction(byte nodeId, int count, IDeviceService devices)
        {
            this._devices = devices;
            this.BlinkCount = count;
            this.NodeId = nodeId;
        }

        public int BlinkCount { get; set; }
        public byte NodeId { get; set; }

        public override void Execute(NetworkEvent sourceEvent)
        {
            BlinkAnimation anim = new BlinkAnimation(this._devices.Lamps.First(item => item.NodeID == this.NodeId));
            anim.BlinkCount = this.BlinkCount;
            anim.Start();
        }
    }
}
