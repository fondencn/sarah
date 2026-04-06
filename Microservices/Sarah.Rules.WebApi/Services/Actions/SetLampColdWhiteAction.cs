using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;

namespace Sarah.Rules.Actions
{
    public class SetLampColdWhiteAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        private byte TargetNodeId { get; set; }

        public SetLampColdWhiteAction(byte targetNodeId, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this.TargetNodeId = targetNodeId;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            await _deviceServiceClient.SetLampColdWhiteByNodeAsync(this.TargetNodeId);
        }
    }
}
