using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Text;
using Sarah.API.BusinessObjects.SpeakerRequests;
using Services.Sarah.API.Interfaces.Service;

namespace Sarah.Voice.DeviceApi
{
    public class DeviceServiceClient : IDeviceServiceClient
    {

        private Uri BaseUri {get;}
        private readonly ILogger? _logger;
        
        public DeviceServiceClient(string serverName, ILogger<DeviceServiceClient>? logger = null)
        {
            BaseUri = new Uri("http://" + serverName + ":5000");
            _logger = logger;
        }


        public void Initialize()
        {
            this.BeginEventApiActivíty();
        }
        
        private void BeginEventApiActivíty()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await this.RegisterSpeaker();
                    }
                    catch
                    {
                        _logger?.LogWarning("Zentrale der Speaker Event API konnte nicht erreicht werden. Probiere es in 5 Minuten wieder.");
                    }
                    await Task.Delay(5 * 60 * 1000);
                }
            });
        }

        public async Task ToggleLampByRoom(string roomName, string lampName)
        {
            HttpClient http = new HttpClient();

            SetLampByRoomRequest request = new SetLampByRoomRequest()
            {
                RoomName = roomName,
                LampName = lampName,
                On = false
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(BaseUri, "/api/DeviceApi/ToggleLampByRoom");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task SetLampByRoom(string roomName, string lampName, bool on)
        {
            HttpClient http = new HttpClient();

            SetLampByRoomRequest request = new SetLampByRoomRequest()
            {
                RoomName = roomName,
                LampName = lampName,
                On = on
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(BaseUri, "/api/DeviceApi/SetLampByRoom");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task SetTemperatureByRoom(string roomName, float temperature)
        {
            HttpClient http = new HttpClient();

            SetTemperatureByRoomRequest request = new SetTemperatureByRoomRequest()
            {
                RoomName = roomName,
                Temperature = temperature
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(BaseUri, "/api/DeviceApi/SetTemperatureByRoom");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }


        public async Task<GetOpenDoorsResponse> GetOpenDoors()
        {
            HttpClient http = new HttpClient();

            Uri uri = new Uri(BaseUri, "/api/DeviceApi/GetOpenDoors");
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            GetOpenDoorsResponse responseContent = JsonConvert.DeserializeObject<GetOpenDoorsResponse>(json)!;

            return responseContent;
        }

        public async Task<GetWeatherResponse> GetWeatherInfo()
        {
            HttpClient http = new HttpClient();

            Uri uri = new Uri(BaseUri, "/api/DeviceApi/GetWeatherInfo");
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            GetWeatherResponse responseContent = JsonConvert.DeserializeObject<GetWeatherResponse>(json)!;

            return responseContent;
        }

        public async Task<GetDeseaseInfoResponse> GetDeseaseInfo()
        {
            HttpClient http = new HttpClient();

            Uri uri = new Uri(BaseUri, "/api/DeviceApi/GetDeseaseInfo");
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            GetDeseaseInfoResponse responseContent = JsonConvert.DeserializeObject<GetDeseaseInfoResponse>(json)!;

            return responseContent;
        }


        public async Task SetAlarmSchedule(string text, DateTime alarmTime, string speakerHostname)
        {
            HttpClient http = new HttpClient();

            SetAlarmScheduleRequest request = new SetAlarmScheduleRequest()
            {
                Text = text,
                AlarmTime = alarmTime,
                TargetSpeaker = speakerHostname
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(BaseUri, "/api/DeviceApi/SetAlarmSchedule");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<GetAlarmSchedulesResponse> GetAlarmSchedules()
        {
            HttpClient http = new HttpClient();

            Uri uri = new Uri(BaseUri, "/api/DeviceApi/GetAlarmSchedules");
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            GetAlarmSchedulesResponse responseContent = JsonConvert.DeserializeObject<GetAlarmSchedulesResponse>(json)!;

            return responseContent;
        }


        public async Task<GetPersonLocationResponse> GetPersonLocation(string personName)
        {
            HttpClient http = new HttpClient();

            Uri uri = new Uri(BaseUri, "/api/DeviceApi/GetPersonLocation?personName=" + personName);
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            GetPersonLocationResponse responseContent = JsonConvert.DeserializeObject<GetPersonLocationResponse>(json)!;

            return responseContent;
        }

        public async Task ActivateScene(string sceneName)
        {
            HttpClient http = new HttpClient();

            ActivateSceneRequest request = new ActivateSceneRequest()
            {
                SceneName = sceneName
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(BaseUri, "/api/DeviceApi/ActivateScene");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }


        public async Task DeactivateScene(string sceneName)
        {
            HttpClient http = new HttpClient();

            DeactivateSceneRequest request = new DeactivateSceneRequest()
            {
                SceneName = sceneName
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            Uri uri = new Uri(BaseUri, "/api/DeviceApi/DeactivateScene");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task RegisterSpeaker()
        {
            HttpClient http = new HttpClient();

            var content = new StringContent(JsonConvert.SerializeObject(Environment.MachineName), Encoding.UTF8, "application/json");
            Uri uri = new Uri(BaseUri, "/api/DeviceApi/RegisterSpeaker");
            _logger?.LogDebug("HTTP POST To " + uri);
            HttpResponseMessage response = await http.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<GetWeatherResponse> GetWeatherForecastInfo(DateTime targetDate)
        {
            HttpClient http = new HttpClient();

            Uri uri = new Uri(BaseUri, "/api/DeviceApi/GetWeatherForecastInfo?date=" + targetDate.ToString("dd.MM.yyyy HH:mm"));
            _logger?.LogDebug("HTTP GET To " + uri);
            HttpResponseMessage response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            GetWeatherResponse responseContent = JsonConvert.DeserializeObject<GetWeatherResponse>(json)!;

            return responseContent;
        }
    }
}
