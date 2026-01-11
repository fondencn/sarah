using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using System.Linq;

namespace Sarah.Rules.Actions
{
    /// <summary>
    /// Action die die Steckdose einschaltet
    /// </summary>
    public class WallPlugOnAction : RuleAction
    {
        private readonly IDeviceService _devices;
        public byte TargetNodeId { get; }

        public WallPlugOnAction(byte nodeid, IDeviceService devices)
        {
            this._devices = devices;
            this.TargetNodeId = nodeid;
        }


        public override async void Execute(NetworkEvent sourceEvent)
        {
            IWallPlug device = _devices.WallPlugs.FirstOrDefault(item => item.NodeID == this.TargetNodeId);
            if (device != null)
            {
                await device.SetState(true);
            }
        }
    }


    /// <summary>
    /// Action die die Steckdose ausschaltet
    /// </summary>
    public class WallPlugOffAction : RuleAction
    {
        private readonly IDeviceService _devices;
        public byte TargetNodeId { get; }

        public WallPlugOffAction(byte nodeid, IDeviceService devices)
        {
            this._devices = devices;
            this.TargetNodeId = nodeid;
        }


        public override async void Execute(NetworkEvent sourceEvent)
        {
            await _devices.WallPlugs.First(item => item.NodeID == this.TargetNodeId).SetState(false);
        }
    }
}
