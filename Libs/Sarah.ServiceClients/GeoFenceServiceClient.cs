using System.Net.Http.Json;
using System.Globalization;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Microsoft.Extensions.Logging;

namespace Sarah.ServiceClients;

public class GeoFenceServiceClient : IGeoFenceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeoFenceServiceClient> _logger;

    public GeoFenceServiceClient(HttpClient httpClient, ILogger<GeoFenceServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public IGeoFence? GetCurrent(LocatorPosition pos)
    {
        try
        {
            var latitude = Uri.EscapeDataString(pos.Latitude.Value.ToString("R", CultureInfo.InvariantCulture));
            var longitude = Uri.EscapeDataString(pos.Longtitude.Value.ToString("R", CultureInfo.InvariantCulture));
            var response = _httpClient.GetAsync($"api/geofences/current?latitude={latitude}&longitude={longitude}").Result;
            if (response.IsSuccessStatusCode)
            {
                var geofence = response.Content.ReadFromJsonAsync<GeoFenceDto>().Result;
                return geofence;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Failed to get current geofence: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GeoFenceService for current geofence");
            return null;
        }
    }

    public IGeoFence GetZuhause()
    {
        try
        {
            var response = _httpClient.GetAsync("api/geofences/home").Result;
            if (response.IsSuccessStatusCode)
            {
                var geofence = response.Content.ReadFromJsonAsync<GeoFenceDto>().Result;
                return geofence ?? throw new InvalidOperationException("Home geofence not found");
            }

            _logger.LogError("Failed to get home geofence: {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Failed to get home geofence: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GeoFenceService for home geofence");
            throw;
        }
    }

    public IEnumerable<IGeoFence> GetAll()
    {
        try
        {
            var response = _httpClient.GetAsync("api/geofences/all").Result;
            if (response.IsSuccessStatusCode)
            {
                var geofences = response.Content.ReadFromJsonAsync<List<GeoFenceDto>>().Result;
                return geofences ?? new List<GeoFenceDto>();
            }

            _logger.LogWarning("Failed to get all geofences: {StatusCode}", response.StatusCode);
            return new List<GeoFenceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GeoFenceService for all geofences");
            return new List<GeoFenceDto>();
        }
    }
}

public class GeoFenceDto : IGeoFence
{
    public string Name { get; set; } = string.Empty;
}
