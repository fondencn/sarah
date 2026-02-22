using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Monitoring.WebApi.Data.Entities;

namespace Sarah.Monitoring.WebApi.Extensions;

public static class EntityExtensions
{
    /// <summary>
    /// Extension method to get network item from DeviceInfoEntity
    /// </summary>
    public static INetworkElement? GetNetworkItem(this DeviceInfoEntity device, IDeviceService deviceService)
    {
        return deviceService.GetNetworkItem(device.NodeID);
    }
}
