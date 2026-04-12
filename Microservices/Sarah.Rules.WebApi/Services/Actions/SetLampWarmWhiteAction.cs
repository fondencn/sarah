using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;

namespace Sarah.Rules.Actions
{
    public class SetLampWarmWhiteAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        private byte TargetNodeId { get; set; }

        public SetLampWarmWhiteAction(byte targetNodeId, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this.TargetNodeId = targetNodeId;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            await _deviceServiceClient.SetLampWarmWhiteByNodeAsync(this.TargetNodeId);
        }
    }
}
