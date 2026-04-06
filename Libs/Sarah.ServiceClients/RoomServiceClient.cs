using Sarah.API.BusinessObjects.DTOs;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace Sarah.ServiceClients
{
    public class RoomServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RoomServiceClient> _logger;

        public RoomServiceClient(HttpClient httpClient, ILogger<RoomServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private Uri GetBaseUri() => _httpClient.BaseAddress ?? throw new InvalidOperationException("No HTTP client available");

        public async Task<IReadOnlyList<RoomDto>> GetAllRoomsAsync()
        {
            Uri uri = new Uri(GetBaseUri(), "/api/Rooms");
            _logger.LogDebug("HTTP GET To {Uri}", uri);

            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<RoomDto>>(json) ?? new List<RoomDto>();
        }
    }
}
