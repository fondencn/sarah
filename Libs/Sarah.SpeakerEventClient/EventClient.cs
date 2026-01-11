using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Sarah.API.BusinessObjects.SpeakerEvents;

namespace Sarah.Voice.SpeakerEventClient
{
    public class EventClient
    {
        //#if DEBUG
        //        private static readonly string BaseUri = "https://localhost:44348";
        //#else
        //        private static readonly string BaseUri = "http://{0}:5001";
        //#endif


        private static readonly string BaseUri = "http://{0}:5001";

        public string Hostname { get; }
        public DateTime LastActivity { get; set; }
        private readonly ILogger? _logger;

        public EventClient(string hostname, ILogger<EventClient>? logger = null)
        {
            this.Hostname = hostname;
            this.LastActivity = DateTime.Now;
            _logger = logger;
        }

        public async Task Say(SayRequest request)
        {
            try
            {
                HttpClient http = new HttpClient();

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                Uri uri = new Uri(new Uri(String.Format(BaseUri, request.Hostname)), "/Speech/Say");
                _logger?.LogDebug("HTTP POST To {Uri}", uri);
                HttpResponseMessage response = await http.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Cannot say to {Hostname}", request.Hostname);
            }
        }

        public async Task StartPlaySound(PlaySoundRequest request)
        {
            try
            {
                HttpClient http = new HttpClient();

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                Uri uri = new Uri(new Uri(String.Format(BaseUri, request.Hostname)), "/Speech/StartAudio");
                _logger?.LogDebug("HTTP POST To {Uri}", uri);
                HttpResponseMessage response = await http.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Cannot play audio to {Hostname}", request.Hostname);
            }
        }

        public async Task StopPlaySound(StopSoundRequest request)
        {
            try
            {
                HttpClient http = new HttpClient();

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                Uri uri = new Uri(new Uri(String.Format(BaseUri, request.Hostname)), "/Speech/StopAudio");
                _logger?.LogDebug("HTTP POST To {Uri}", uri);
                HttpResponseMessage response = await http.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Cannot stop audio to {Hostname}", request.Hostname);
            }
        }
    }
}
