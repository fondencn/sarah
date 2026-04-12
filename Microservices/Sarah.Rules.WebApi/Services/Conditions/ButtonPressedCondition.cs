using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class ButtonPressedCondition : RuleCondition
    {
        public byte? SceneId { get; }

        public ButtonPressedCondition(byte buttonNodeId) : this(buttonNodeId, null)
        {
        }

        public ButtonPressedCondition(byte buttonNodeId, byte? sceneid) : base(buttonNodeId)
        {
            this.SceneId = sceneid;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is not ClickedEvent clickedEvt)
                return false;

            bool isMatch = clickedEvt.SourceNodeId == this.TargetNodeId;
            if (this.SceneId.HasValue)
            {
                isMatch &= clickedEvt.SceneId == this.SceneId.Value;
            }
            return isMatch;
        }
    }
}
