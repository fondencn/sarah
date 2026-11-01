using Sarah.API.BusinessObjects;
using System.Collections.Generic;

namespace Sarah.API.Interfaces
{
    public interface INodeFactory
    {
        public NetworkElement? CreateByNodeId(byte nodeId);
        public IEnumerable<byte> GetNonZwaveNodeIds();
        /// <summary>True if the node id is mapped to a concrete element type (defect elements count as known, UnknownElement does not).</summary>
        public bool IsKnownNodeId(byte nodeId);
    }
}
