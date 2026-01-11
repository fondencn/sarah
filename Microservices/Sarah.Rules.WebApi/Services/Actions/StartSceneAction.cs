using Sarah.API.BusinessObjects;
using Sarah.Rules.Clients;
using Sarah.Rules.DTOs.DeviceCommands;
using System;
using System.Threading.Tasks;

namespace Sarah.Rules.Actions
{
    public class StartSceneAction : RuleAction
    {
        private readonly IDeviceServiceClient _deviceServiceClient;
        private readonly string _sceneTypeName;
        private RuleCondition _delayCondition;
        private TimeSpan _delay;

        public StartSceneAction(string sceneTypeName, IDeviceServiceClient deviceServiceClient) 
            : this(sceneTypeName, TimeSpan.Zero, null, deviceServiceClient)
        {
        }

        public StartSceneAction(string sceneTypeName, TimeSpan delay, RuleCondition delayCondition, IDeviceServiceClient deviceServiceClient)
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
            
            var command = new StartSceneCommand
            {
                SceneTypeName = _sceneTypeName
            };
            
            await _deviceServiceClient.StartSceneAsync(command);
        }
    }
}
