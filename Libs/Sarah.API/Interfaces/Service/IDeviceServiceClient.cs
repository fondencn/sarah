using System;
using System.Threading.Tasks;
using Sarah.API.BusinessObjects.SpeakerRequests;

namespace Services.Sarah.API.Interfaces.Service {

    public interface IDeviceServiceClient
    {
        Task SetAlarmSchedule(string text, DateTime alarmTime, string speakerHostname);
        Task SetTemperatureByRoom(string roomName, float temperature);
        Task<GetOpenDoorsResponse> GetOpenDoors();
        Task RegisterSpeaker();
        Task SetLampByRoom(string roomName, string lampName, bool on);
        Task ToggleLampByRoom(string roomName, string lampName);
        void Initialize();
        Task ActivateScene(string sceneName);
        Task DeactivateScene(string sceneName);
        Task<GetDeseaseInfoResponse> GetDeseaseInfo();
        Task<GetAlarmSchedulesResponse> GetAlarmSchedules();
        Task<GetPersonLocationResponse> GetPersonLocation(string person);
    }
}