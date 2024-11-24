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
    }
}
