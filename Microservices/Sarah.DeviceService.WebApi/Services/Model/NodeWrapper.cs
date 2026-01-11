using System.Threading.Tasks;
using Sarah.API.Interfaces;
using ZWave;

namespace Sarah.DeviceService.Model
{
    internal class NodeWrapper : INode
    {
        private readonly Node _Node;
        internal NodeWrapper(Node wrappedItem)
        {
            this._Node = wrappedItem;
        }

        public async Task<string> GetDeviceTypeName()
        {
            var proto = await _Node.GetProtocolInfo();
            return proto.GenericType.ToString();
        }
    }
}
