using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.API.BusinessObjects.SpeakerRequests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces.Services
{
    public interface IDeviceService : ICanSelfTest
    {
        INode? GetNode(byte nodeId);

        Task Start();

        public IEnumerable<ILamp> Lamps { get; }

        public IEnumerable<IWallPlug> WallPlugs { get; }

        public IEnumerable<IMultiSensor> Sensors { get; }
        public IEnumerable<IDoorSensor> DoorSensors { get; }
        public IEnumerable<ISmokeSensor> SmokeSensors { get; }
        public IEnumerable<IBatterySensor> BatterySensors { get; }
        public IEnumerable<IThermoElement> Heatings { get; }
        public IEnumerable<IControllerElement> Controllers { get; }
        public IEnumerable<IWallController> WallControllers { get; }
        public IEnumerable<IUnknownElement> UnknownElements { get; }
        public IEnumerable<IGPSTracker> GPSTrackers { get; }

        string SerialPortName { get; }
        string StatusMessage { get; }

        IEnumerable<NetworkElement> Elements { get; }

        INetworkElement? GetNetworkItem(byte sourceNodeId);
        IParameterProvider?GetParameterProvider(KnownDeviceTypes specificType);

        Task<IAssociationGroup[]> GetAssociationGroups(byte nodeID);
        Task SetAssociationGroup(byte nodeID, byte groupId, byte[] nodeIds);

        // High-level API methods for Speaker/HTTP clients
        Task ToggleLampByRoom(string roomName, string lampName);
        Task SetLampByRoom(string roomName, string lampName, bool on);
        Task SetTemperatureByRoom(string roomName, float temperature);
        Task<GetOpenDoorsResponse> GetOpenDoors();
        Task<GetDeseaseInfoResponse> GetDeseaseInfo();
        Task SetAlarmSchedule(string text, DateTime alarmTime, string speakerHostname);
        Task<GetAlarmSchedulesResponse> GetAlarmSchedules();
        Task<GetPersonLocationResponse> GetPersonLocation(string personName);
        Task<TrackerDto?> GetGpsTrackerByNodeId(byte nodeId);
        Task ActivateScene(string sceneName);
        Task DeactivateScene(string sceneName);
    }
}
