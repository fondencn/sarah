using Sarah.Logging;
using Newtonsoft.Json;
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

        public EventClient(string hostname)
        {
            this.Hostname = hostname;
            this.LastActivity = DateTime.Now;
        }

        public async Task Say(SayRequest request)
        {
            try
            {
                HttpClient http = new HttpClient();

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                Uri uri = new Uri(new Uri(String.Format(BaseUri, request.Hostname)), "/Event/Say");
                Logger.Instance.LogDebug("HTTP POST To " + uri);
                HttpResponseMessage response = await http.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Cannot say to " + request.Hostname, ex);
            }
        }

        public async Task StartPlaySound(PlaySoundRequest request)
        {
            try
            {
                HttpClient http = new HttpClient();

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                Uri uri = new Uri(new Uri(String.Format(BaseUri, request.Hostname)), "/Event/StartAudio");
                Logger.Instance.LogDebug("HTTP POST To " + uri);
                HttpResponseMessage response = await http.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Cannot play audio to " + request.Hostname, ex);
            }
        }

        public async Task StopPlaySound(StopSoundRequest request)
        {
            try
            {
                HttpClient http = new HttpClient();

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                Uri uri = new Uri(new Uri(String.Format(BaseUri, request.Hostname)), "/Event/StopAudio");
                Logger.Instance.LogDebug("HTTP POST To " + uri);
                HttpResponseMessage response = await http.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Cannot stop audio to " + request.Hostname, ex);
            }
        }
    }
}
