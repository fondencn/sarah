using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;
using System;
using System.Threading.Tasks;

namespace Sarah.Rules.Actions
{
    public class ToggleLampAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        private byte TargetNodeId { get; set; }

        public ToggleLampAction(byte targetNodeId, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this.TargetNodeId = targetNodeId;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            _ = _deviceServiceClient.ToggleLampByNodeAsync(this.TargetNodeId);
        }
    }
}
