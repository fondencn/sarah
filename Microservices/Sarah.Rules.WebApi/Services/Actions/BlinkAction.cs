using Sarah.API.BusinessObjects;
using Sarah.Rules.Clients;
using Sarah.Rules.DTOs.DeviceCommands;

namespace Sarah.Rules.Actions
{
    public class BlinkAction : RuleAction
    {
        private readonly IDeviceServiceClient _deviceServiceClient;

        public BlinkAction(byte nodeId, int count, IDeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this.BlinkCount = count;
            this.NodeId = nodeId;
        }

        public int BlinkCount { get; set; }
        public byte NodeId { get; set; }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            var command = new BlinkAnimationCommand
            {
                NodeId = this.NodeId,
                BlinkCount = this.BlinkCount
            };
            
            await _deviceServiceClient.ExecuteBlinkAnimationAsync(command);
        }
    }
}
