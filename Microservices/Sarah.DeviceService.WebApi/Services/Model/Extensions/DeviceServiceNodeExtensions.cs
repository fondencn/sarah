using Sarah.API.Interfaces.Services;
using ZWave;

namespace Sarah.DeviceService.Model.Extensions
{
    internal static class DeviceServiceNodeExtensions
    {
        internal static Node? GetZWaveNode(this IDeviceService deviceService, byte nodeId)
        {
            if (deviceService is DeviceService concreteService)
            {
                return concreteService.GetNodeInternal(nodeId);
            }

            if (deviceService.GetNode(nodeId) is NodeWrapper wrapped)
            {
                return wrapped.WrappedNode;
            }

            return null;
        }
    }
}