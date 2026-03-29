using Sarah.API.Interfaces;
using ZWave;

namespace Sarah.DeviceService.Model
{
    internal class NodeWrapper : INode
    {
        private readonly Node? _Node;
        internal Node? WrappedNode => _Node;

        internal NodeWrapper(Node? wrappedItem)
        {
            this._Node = wrappedItem;
        }

        public async Task<string> GetDeviceTypeName()
        {
            if(_Node == null) return string.Empty;
            else 
            {
                var proto = await _Node.GetProtocolInfo();
                return proto.GenericType.ToString();
            }
        }
    }
}
