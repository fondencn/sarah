using Sarah.Monitoring.DTOs;
using System.Text.Json;

namespace Sarah.Monitoring.Clients
{
    public class RulesServiceClient : IRulesServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RulesServiceClient> _logger;

        public RulesServiceClient(HttpClient httpClient, ILogger<RulesServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<RuleStatusDto?> GetStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/rules/status");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<RuleStatusDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to get rules status. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rules status");
                return null;
            }
        }
    }
}
