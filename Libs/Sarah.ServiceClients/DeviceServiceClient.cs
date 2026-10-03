using System.Text;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.API.BusinessObjects.SpeakerRequests;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace Sarah.ServiceClients
{
    public class DeviceServiceClient : IDeviceService
    {
        private readonly ILogger<DeviceServiceClient> _logger;
        private readonly HttpClient _httpClient;
        
        // Constructor for HTTP client registration (required for typed clients)
        public DeviceServiceClient(HttpClient httpClient, ILogger<DeviceServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private Uri GetBaseUri() => _httpClient.BaseAddress ?? throw new InvalidOperationException("No HTTP client available");


        public async Task<GetOpenDoorsResponse> GetOpenDoors()
        {
            HttpClient http = _httpClient;

            Uri uri = new Uri(GetBaseUri(), "/api/devices/open-doors");
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            GetOpenDoorsResponse responseContent = JsonConvert.DeserializeObject<GetOpenDoorsResponse>(json)!;

            return responseContent;
        }

        public async Task ActivateScene(string sceneName)
        {
            HttpClient http = _httpClient;

            var request = new
            {
                SceneTypeName = sceneName
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(GetBaseUri(), "/api/devices/scenes/start");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }


        public async Task DeactivateScene(string sceneName)
        {
            HttpClient http = _httpClient;

            var request = new
            {
                SceneTypeName = sceneName
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(GetBaseUri(), "/api/devices/scenes/stop");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<TrackerDto?> GetGpsTrackerByNodeId(byte nodeId)
        {
            HttpClient http = _httpClient;

            Uri uri = new Uri(GetBaseUri(), $"/api/Devices/gpstracker/{nodeId}");
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            TrackerDto responseContent = JsonConvert.DeserializeObject<TrackerDto>(json)!;

            return responseContent;
        }

        public async Task SetLampBrightness(long deviceId, byte brightness)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/Devices/lamp/{deviceId}/brightness/{brightness}");
            _logger?.LogDebug("HTTP POST To " + uri);

            HttpResponseMessage response = await _httpClient.PostAsync(uri, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<DeviceDto?> GetDeviceByIdAsync(long id)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/{id}");
            _logger?.LogDebug("HTTP GET To " + uri);

            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            DeviceDto responseContent = JsonConvert.DeserializeObject<DeviceDto>(json)!;

            return responseContent;
        }

        public async Task<DeviceDto?> GetDeviceByNodeIdAsync(byte nodeId)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/bynode/{nodeId}");
            _logger?.LogDebug("HTTP GET To " + uri);

            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DeviceDto>(json);
        }

        public async Task SetThermostatTemperatureAsync(long deviceId, float temperature)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/thermostat/{deviceId}/temperature/{temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await _httpClient.PostAsync(uri, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<float?> GetRoomAverageTemperatureAsync(long roomId)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/room/{roomId}/avgtemperature");
            _logger?.LogDebug("HTTP GET To " + uri);

            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<float>(json);
        }

        public async Task ToggleLampByNodeAsync(byte nodeId)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/bynode/{nodeId}/lamp/toggle");
            _logger?.LogDebug("HTTP POST To " + uri);
            (await _httpClient.PostAsync(uri, null)).EnsureSuccessStatusCode();
        }

        public async Task SetLampWarmWhiteByNodeAsync(byte nodeId)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/bynode/{nodeId}/lamp/warmwhite");
            _logger?.LogDebug("HTTP POST To " + uri);
            (await _httpClient.PostAsync(uri, null)).EnsureSuccessStatusCode();
        }

        public async Task SetLampColdWhiteByNodeAsync(byte nodeId)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/bynode/{nodeId}/lamp/coldwhite");
            _logger?.LogDebug("HTTP POST To " + uri);
            (await _httpClient.PostAsync(uri, null)).EnsureSuccessStatusCode();
        }

        public async Task SetLampColorAndBrightnessByNodeAsync(byte nodeId, string color, byte brightness)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/bynode/{nodeId}/lamp/color/{Uri.EscapeDataString(color)}/brightness/{brightness}");
            _logger?.LogDebug("HTTP POST To " + uri);
            (await _httpClient.PostAsync(uri, null)).EnsureSuccessStatusCode();
        }

        public async Task SetWallPlugStateByNodeAsync(byte nodeId, bool isOn)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/bynode/{nodeId}/wallplug/state/{isOn.ToString().ToLowerInvariant()}");
            _logger?.LogDebug("HTTP POST To " + uri);
            (await _httpClient.PostAsync(uri, null)).EnsureSuccessStatusCode();
        }

        public async Task ToggleWallPlugByNodeAsync(byte nodeId)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/bynode/{nodeId}/wallplug/toggle");
            _logger?.LogDebug("HTTP POST To " + uri);
            (await _httpClient.PostAsync(uri, null)).EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<DeviceDto>> GetAllDevicesAsync()
        {
            Uri uri = new Uri(GetBaseUri(), "/api/Devices");
            _logger?.LogDebug("HTTP GET To " + uri);

            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<DeviceDto>>(json) ?? new List<DeviceDto>();
        }

        public async Task<RoomSummaryDto?> GetRoomSummaryAsync(long roomId)
        {
            Uri uri = new Uri(GetBaseUri(), $"/api/devices/room/{roomId}/summary");
            _logger?.LogDebug("HTTP GET To " + uri);

            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RoomSummaryDto>(json);
        }

        // IDeviceService properties not supported over HTTP — all callers must use typed HTTP methods instead
        public IEnumerable<ILamp> Lamps => throw new NotImplementedException("Lamps is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IWallPlug> WallPlugs => throw new NotImplementedException("WallPlugs is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IMultiSensor> Sensors => throw new NotImplementedException("Sensors is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IDoorSensor> DoorSensors => throw new NotImplementedException("DoorSensors is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<ISmokeSensor> SmokeSensors => throw new NotImplementedException("SmokeSensors is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IBatterySensor> BatterySensors => throw new NotImplementedException("BatterySensors is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IThermoElement> Heatings => throw new NotImplementedException("Heatings is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IControllerElement> Controllers => throw new NotImplementedException("Controllers is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IWallController> WallControllers => throw new NotImplementedException("WallControllers is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IUnknownElement> UnknownElements => throw new NotImplementedException("UnknownElements is not available over HTTP; use typed HTTP methods instead.");
        public IEnumerable<IGPSTracker> GPSTrackers => throw new NotImplementedException("GPSTrackers is not available over HTTP; use typed HTTP methods instead.");
        
        public string SerialPortName => "N/A - HTTP Client";
        public string StatusMessage => "DeviceServiceClient - HTTP-based client";
        
        public IEnumerable<NetworkElement> Elements => Enumerable.Empty<NetworkElement>();
        
        public INode? GetNode(byte nodeId)
        {
            _logger?.LogWarning("GetNode not implemented in HTTP client");
            return null;
        }
        
        public Task Start()
        {
            return Task.CompletedTask;
        }
        
        public INetworkElement? GetNetworkItem(byte sourceNodeId)
        {
            _logger?.LogWarning("GetNetworkItem not implemented in HTTP client");
            return null;
        }
        
        public IParameterProvider? GetParameterProvider(KnownDeviceTypes specificType)
        {
            _logger?.LogWarning("GetParameterProvider not implemented in HTTP client");
            return null;
        }
        
        public Task<IAssociationGroup[]> GetAssociationGroups(byte nodeID)
        {
            _logger?.LogWarning("GetAssociationGroups not implemented in HTTP client");
            return Task.FromResult(Array.Empty<IAssociationGroup>());
        }
        
        public Task SetAssociationGroup(byte nodeID, byte groupId, byte[] nodeIds)
        {
            _logger?.LogWarning("SetAssociationGroup not implemented in HTTP client");
            return Task.CompletedTask;
        }
        
        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            _logger?.LogInformation("RunSelfTest - HTTP client self-test");
            return Enumerable.Empty<SelfTestResult>();
        }
    }
}
