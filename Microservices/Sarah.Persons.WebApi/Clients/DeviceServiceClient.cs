using System.Net.Http.Json;

namespace Sarah.Persons.WebApi.Clients;

public interface IDeviceServiceClient
{
    Task<DeviceDto?> GetDeviceByIdAsync(long id);
    Task<DeviceDto?> GetGpsTrackerByNodeIdAsync(byte nodeId);
}

public class DeviceServiceClient : IDeviceServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeviceServiceClient> _logger;

    public DeviceServiceClient(HttpClient httpClient, ILogger<DeviceServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DeviceDto?> GetDeviceByIdAsync(long id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/devices/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DeviceDto>();
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Failed to get device {DeviceId}: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DeviceService for device {DeviceId}", id);
            return null;
        }
    }

    public async Task<DeviceDto?> GetGpsTrackerByNodeIdAsync(byte nodeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/devices/gpstracker/{nodeId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DeviceDto>();
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Failed to get GPS tracker with NodeID {NodeId}: {StatusCode}", nodeId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DeviceService for GPS tracker {NodeId}", nodeId);
            return null;
        }
    }
}
