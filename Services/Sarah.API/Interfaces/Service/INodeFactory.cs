using Sarah.API.BusinessObjects;
using System.Collections.Generic;

namespace Sarah.API.Interfaces
{
    public interface INodeFactory
    {
        public NetworkElement CreateByNodeId(byte nodeId);
        public IEnumerable<byte> GetNonZwaveNodeIds();
    }
}
