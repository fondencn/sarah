using Sarah.API.BusinessObjects;
using Sarah.DeviceService.Model.Animations;
using System;
using System.Threading.Tasks;

namespace Sarah.Rules.Actions
{
    public class StartSceneAction : RuleAction
    {
        private readonly Type _sceneType;

        private RuleCondition _delayCondition;

        private TimeSpan _delay;

        public StartSceneAction(Type sceneType) : this(sceneType, TimeSpan.Zero, null)
        {
        }

        public StartSceneAction(Type sceneType, TimeSpan delay,  RuleCondition delayCondition)
        {
            this._sceneType = sceneType;
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
            Scene.Start(this._sceneType);
        }
    }
}
