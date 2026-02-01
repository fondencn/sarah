using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using System;

namespace Sarah.Rules.Actions
{
    public class StopSceneAction : RuleAction
    {
        private readonly IDeviceService _deviceServiceClient;
        private readonly string _sceneTypeName;
        
        public StopSceneAction(string sceneTypeName, IDeviceService deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this._sceneTypeName = sceneTypeName;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            var command = new StopSceneCommand
            {
                SceneTypeName = _sceneTypeName
            };
            
            await _deviceServiceClient.StopSceneAsync(command);
        }
    }
}
