using Sarah.API.BusinessObjects;
using Sarah.ServiceClients;
using System;

namespace Sarah.Rules.Actions
{
    public class StopSceneAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        private readonly string _sceneTypeName;
        
        public StopSceneAction(string sceneTypeName, DeviceServiceClient deviceServiceClient)
        {
            this._deviceServiceClient = deviceServiceClient;
            this._sceneTypeName = sceneTypeName;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            await _deviceServiceClient.DeactivateScene(_sceneTypeName);
        }
    }
}
