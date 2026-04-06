using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class TrackerButtonPressedCondition : RuleCondition
    {
        public byte? SceneId { get; }

        public TrackerButtonPressedCondition(byte buttonNodeId) : this(buttonNodeId, null)
        {
        }

        public TrackerButtonPressedCondition(byte buttonNodeId, byte? sceneid) : base(buttonNodeId)
        {
            this.SceneId = sceneid;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is not TrackerButtonPressedEvent trackerEvt)
                return false;

            return trackerEvt.SourceNodeId == this.TargetNodeId && trackerEvt.IsPressed;
        }
    }
}
