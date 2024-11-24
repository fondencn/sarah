namespace Sarah.API.BusinessObjects
{
    public abstract class RuleCondition
    {
        public byte TargetNodeId { get; }

        protected RuleCondition(byte targetNodeId)
        {
            this.TargetNodeId = targetNodeId;
        }

        public abstract bool Evaluate(NetworkEvent evt);
    }
}
