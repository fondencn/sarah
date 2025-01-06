using Sarah.Server.Models.Dtos;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Logging;
using Sarah.API.Interfaces.Service;

namespace Sarah.Server.Models;

public static class NetworkElementFactory
{
    /// <summary>
    /// Factory
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<NetworkElementDto> Create(IDeviceService deviceService, IDBService database)
    {
        List<NetworkElementDto> networkElements = new List<NetworkElementDto>();
        try
        {
            database.Devices.ToList().ForEach(device =>
            {
                var dto = new NetworkElementDto() 
                { 
                    Name = device.Name, 
                    Info = (device.GetNetworkItem(deviceService)?.StateInfo) ?? "Unknown state    ", 
                    TypeName = device.SpecificType.ToString() + "|" + (device.GetNetworkItem(deviceService)?.GetType().Name ?? "Unknown type"), 
                    ID = device.NodeID
                };
                networkElements.Add(dto);
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.LogDebug(ex.Message);
            networkElements.Add(new NetworkElementDto() { Name = "Fehler", Info = ex.Message });
        }

        return networkElements
            .OrderBy(x => x.ID);
    }
}
