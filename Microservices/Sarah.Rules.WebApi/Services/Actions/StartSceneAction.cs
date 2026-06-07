using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;
using System;
using System.Threading.Tasks;

namespace Sarah.Rules.Actions
{
    public class StartSceneAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        private readonly string _sceneTypeName;
        private RuleCondition? _delayCondition;
        private TimeSpan _delay;

        public StartSceneAction(string sceneTypeName, DeviceServiceClient deviceServiceClient) 
            : this(sceneTypeName, TimeSpan.Zero, null, deviceServiceClient)
        {
        }

        public StartSceneAction(string sceneTypeName, TimeSpan delay, RuleCondition? delayCondition, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this._sceneTypeName = sceneTypeName;
            this._delayCondition = delayCondition;
            this._delay = delay;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            await Task.Delay(this._delay);
            if (this._delayCondition != null && !this._delayCondition.Evaluate(sourceEvent))
            {
                return;
            }
            
            await _deviceServiceClient.ActivateScene(_sceneTypeName);
        }
    }
}
