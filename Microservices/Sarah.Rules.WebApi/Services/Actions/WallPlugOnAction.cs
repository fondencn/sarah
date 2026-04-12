using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.ServiceClients;

namespace Sarah.Rules.Actions
{
    public class WallPlugOnAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        public byte TargetNodeId { get; }

        public WallPlugOnAction(byte nodeid, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this.TargetNodeId = nodeid;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            await _deviceServiceClient.SetWallPlugStateByNodeAsync(this.TargetNodeId, true);
        }
    }

    public class WallPlugOffAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        public byte TargetNodeId { get; }

        public WallPlugOffAction(byte nodeid, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this.TargetNodeId = nodeid;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            await _deviceServiceClient.SetWallPlugStateByNodeAsync(this.TargetNodeId, false);
        }
    }
}
