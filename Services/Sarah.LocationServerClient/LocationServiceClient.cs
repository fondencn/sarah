using InteLuk.API.BusinessObjects;
using InteLuk.API.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace InteLuk.LocationServiceClient
{
    public class LocationServiceClient : ILocationService
    {
        private HttpClient _http = null;
        private HttpClient Http
        {
            get
            {
                if (_http == null)
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        //bypass
                        return true;
                    };
                    _http = new HttpClient(handler);
                }
                return _http;
            }
        }

        private ILocationClientSettings Settings { get; }


        public LocationServiceClient(ILocationClientSettings settings)
        {
            this.Settings = settings;
        }


        public async Task AddLocation(ProtectedLocationServiceEntry item)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                //bypass
                return true;
            };

            string url = Settings.Url + "/api/LocationService";
            item.ApiKey = LocationServiceApiKey.ApiKey;
            string json = JsonSerializer.Serialize(item);
            StringContent content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await this.Http.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<LocationServiceEntry> GetLocation(string personName, string apiKey)
        {
            string url = Settings.Url + "/api/LocationService/Location?personName=" + personName + "&apiKey=" + apiKey;
            HttpResponseMessage response = await Http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            LocationServiceEntry result = JsonSerializer.Deserialize<LocationServiceEntry>(json, options);
            return result;
        }

        public async Task<IEnumerable<LocationServiceEntry>> GetLocationTrace(string personName, string apiKey)
        {
            string url = Settings.Url + "/api/LocationService/Trace?personName=" + personName + "&apiKey=" + apiKey;
            HttpResponseMessage response = await Http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            LocationServiceEntry[] result = JsonSerializer.Deserialize<LocationServiceEntry[]>(json, options);
            return result;
        }
    }
}
