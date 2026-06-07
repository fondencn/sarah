using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace Sarah.ServiceClients
{
    public class PersonServiceClient : IPersonService
    {
        private readonly ILogger<PersonServiceClient> _logger;
        private readonly HttpClient _httpClient;
        
        // Constructor for HTTP client registration (required for typed clients)
        public PersonServiceClient(HttpClient httpClient, ILogger<PersonServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private Uri GetBaseUri() => _httpClient.BaseAddress ?? throw new InvalidOperationException("No HTTP client available");

        public async Task AddPersonAsync(IPerson person)
        {
            try
            {
                var personDto = new PersonDto
                {
                    Name = person.Name,
                    GPSTrackerID = person.GPSTrackerID,
                    MobilePhoneHostname = person.MobilePhoneHostname
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(personDto),
                    Encoding.UTF8,
                    "application/json");

                Uri uri = new Uri(GetBaseUri(), "/api/persons");
                _logger?.LogDebug("HTTP POST To " + uri);

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding person: {PersonName}", person.Name);
                throw;
            }
        }

        public async Task DeletePersonAsync(long id)
        {
            try
            {
                Uri uri = new Uri(GetBaseUri(), $"/api/persons/{id}");
                _logger?.LogDebug("HTTP DELETE To " + uri);

                HttpResponseMessage response = await _httpClient.DeleteAsync(uri);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting person with id: {PersonId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<IPerson>> GetAllPersonsAsync()
        {
            try
            {
                Uri uri = new Uri(GetBaseUri(), "/api/persons");
                _logger?.LogDebug("HTTP GET To " + uri);

                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var payload = JsonConvert.DeserializeObject<JArray>(json);
                if (payload == null)
                {
                    return new List<PersonDto>();
                }

                return payload
                    .OfType<JObject>()
                    .Select(MapPersonPayload)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all persons");
                throw;
            }
        }

        public async Task<IPerson?> GetPersonByIdAsync(long id)
        {
            try
            {
                Uri uri = new Uri(GetBaseUri(), $"/api/persons/{id}");
                _logger?.LogDebug("HTTP GET To " + uri);

                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var payload = JsonConvert.DeserializeObject<JObject>(json);
                if (payload == null)
                {
                    return null;
                }

                return MapPersonPayload(payload);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting person with id: {PersonId}", id);
                throw;
            }
        }

        public async Task UpdatePersonAsync(IPerson person)
        {
            try
            {
                var personDto = new PersonDto
                {
                    Id = person.Id,
                    Name = person.Name,
                    GPSTrackerID = person.GPSTrackerID,
                    MobilePhoneHostname = person.MobilePhoneHostname
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(personDto),
                    Encoding.UTF8,
                    "application/json");

                Uri uri = new Uri(GetBaseUri(), $"/api/persons/{person.Id}");
                _logger?.LogDebug("HTTP PUT To " + uri);

                HttpResponseMessage response = await _httpClient.PutAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating person: {PersonName}", person.Name);
                throw;
            }
        }

        public async Task<string[]> GetKnownHomeNetworkDevices()
        {
            try
            {
                Uri uri = new Uri(GetBaseUri(), "/api/persons/known-home-network-devices");
                _logger?.LogDebug("HTTP GET To " + uri);

                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var devices = JsonConvert.DeserializeObject<string[]>(json);

                return devices ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting known home network devices");
                throw;
            }
        }

        private static PersonDto MapPersonPayload(JObject payload)
        {
            var geoFenceName = payload.Value<string>("currentGeoFence");

            return new PersonDto
            {
                Id = payload.Value<long?>("id") ?? 0,
                Name = payload.Value<string>("name") ?? string.Empty,
                GPSTrackerID = payload.Value<byte?>("gpsTrackerID") ?? 0,
                MobilePhoneHostname = payload.Value<string>("mobilePhoneHostname") ?? string.Empty,
                IsAtHome = payload.Value<bool?>("isAtHome") ?? false,
                TrackerDeviceName = payload.Value<string>("gpsTrackerName") ?? string.Empty,
                CurrentGeoFence = string.IsNullOrWhiteSpace(geoFenceName)
                    ? null
                    : new GeoFenceDto { Name = geoFenceName },
                GPSTracker = null, 
                CurrentPositionLong = payload.Value<float?>("currentPositionLong"),
                CurrentPositionLat = payload.Value<float?>("currentPositionLat"),
                CurrentNamedPosition = payload.Value<string>("currentNamedPosition")
            };
        }

        public async Task<bool> IsSomeonePresent()
        {
            try
            {
                Uri uri = new Uri(GetBaseUri(), "/api/persons/is-someone-present");
                _logger?.LogDebug("HTTP GET To " + uri);

                HttpResponseMessage response = await _httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<bool>(json);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if someone is present");
                throw;
            }
        }
    }
}
