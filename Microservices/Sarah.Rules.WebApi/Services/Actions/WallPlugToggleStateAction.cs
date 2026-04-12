using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;

namespace Sarah.Rules.Actions
{
    public class WallPlugToggleStateAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        private byte TargetNodeId { get; set; }

        public WallPlugToggleStateAction(byte targetNodeId, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this.TargetNodeId = targetNodeId;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            await _deviceServiceClient.ToggleWallPlugByNodeAsync(this.TargetNodeId);
        }
    }
}
