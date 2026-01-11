using Sarah.Rules.DTOs.DeviceCommands;
using System.Text;
using System.Text.Json;

namespace Sarah.Rules.Clients
{
    public class DeviceServiceClient : IDeviceServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DeviceServiceClient> _logger;

        public DeviceServiceClient(HttpClient httpClient, ILogger<DeviceServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task ExecuteBlinkAnimationAsync(BlinkAnimationCommand command)
        {
            try
            {
                var json = JsonSerializer.Serialize(command);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/api/devices/animations/blink", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to execute blink animation. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing blink animation for NodeId {NodeId}", command.NodeId);
            }
        }

        public async Task StartSceneAsync(StartSceneCommand command)
        {
            try
            {
                var json = JsonSerializer.Serialize(command);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/api/devices/scenes/start", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to start scene {SceneName}. Status: {StatusCode}", 
                        command.SceneTypeName, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting scene {SceneName}", command.SceneTypeName);
            }
        }

        public async Task StopSceneAsync(StopSceneCommand command)
        {
            try
            {
                var json = JsonSerializer.Serialize(command);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/api/devices/scenes/stop", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to stop scene {SceneName}. Status: {StatusCode}", 
                        command.SceneTypeName, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping scene {SceneName}", command.SceneTypeName);
            }
        }
    }
}
