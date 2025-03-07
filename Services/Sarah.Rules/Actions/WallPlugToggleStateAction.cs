using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using System.Linq;

namespace InteLuk.Rules.Actions
{
    public class WallPlugToggleStateAction : RuleAction
    {
        private readonly IDeviceService _devices;
        private int TargetNodeId { get; set; }

        public WallPlugToggleStateAction(int targetNodeId, IDeviceService devices)
        {
            this._devices = devices;
            this.TargetNodeId = targetNodeId;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            _devices.WallPlugs.First(item => item.NodeID == TargetNodeId).ToggleState();
        }
    }
}
