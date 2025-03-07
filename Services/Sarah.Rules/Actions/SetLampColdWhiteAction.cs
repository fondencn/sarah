using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using System.Linq;

namespace Sarah.Rules.Actions
{
    public class SetLampColdWhiteAction : RuleAction
    {
        private readonly IDeviceService _devices;

        private byte TargetNodeId { get; set; }

        public SetLampColdWhiteAction(byte targetNodeId, IDeviceService devices)
        {
            this._devices = devices;
            this.TargetNodeId = targetNodeId;
        }


        public override async void Execute(NetworkEvent sourceEvent)
        {
            await this._devices.Lamps.First(item => item.NodeID == TargetNodeId).SetColdWhite();
        }
    }
}
