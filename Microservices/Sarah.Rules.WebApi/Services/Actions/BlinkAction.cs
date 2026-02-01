using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Actions
{
    public class BlinkAction : RuleAction
    {
        private readonly IDeviceService _deviceServiceClient;

        public BlinkAction(byte nodeId, int count, IDeviceService deviceServiceClient)
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
