using Sarah.API.BusinessObjects;
using Sarah.DeviceService.Model.Animations;
using System;

namespace Sarah.Rules.Actions
{
    public class StopSceneAction : RuleAction
    {
        private readonly Type _sceneType;
        public StopSceneAction(Type sceneType)
        {
            this._sceneType = sceneType;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
            Scene.Stop(this._sceneType);
        }
    }
}
