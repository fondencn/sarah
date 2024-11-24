using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface IDeviceService : ICanSelfTest
    {
        INode GetNode(byte nodeId);

        Task Start(INodeFactory nodeFactory);

        Task Start(INodeFactory factory, string serialPortName);

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

        INetworkElement GetNetworkItem(byte sourceNodeId);
        IParameterProvider GetParameterProvider(KnownDeviceTypes specificType);

        Task<IAssociationGroup[]> GetAssociationGroups(byte nodeID);
        Task SetAssociationGroup(byte nodeID, byte groupId, byte[] nodeIds);
    }

    public interface INetworkElement
    {
        byte NodeID { get; }
        string StateInfo { get; }
        string ClassDescription { get; }
        bool? IsActive { get; }

        // Task<IAssociationGroup[]> GetAssociationGroups();
        // Task SetAssociationGroup(byte groupId, byte[] newNodes);
    }

    public interface INode
    {

    }

    public interface ILamp : INetworkElement
    {
        DateTime? LastChange { get; }
        byte Brightness { get; }
        string Color { get; }

        Task SetBrightness(byte brightness);
        Task SetColor(string color);
        Task SetWarmWhite();
        Task ToggleState();
        Task SetColdWhite();
        Animation CurrentAnimation { get; set; }
    }

    public interface IWallPlug : INetworkElement
    {
        bool IsOn { get; }
        DateTime LastChangeToPowerLow { get; }
        DateTime LastIncreasePower { get; }
        DateTime LastDecreasePower { get; }

        Task SetState(bool newValue);
        void ToggleState();
    }

    public interface IDoorSensor : INetworkElement
    {
        DoorSensorState State { get; }
    }

    public interface IMultiSensor : INetworkElement
    {
        SensorData Luminance { get; }
        SensorData Presence { get; }
        SensorData RelativeHumidity { get; }
        SensorData VolatileOrganicCompounds { get; }
        SensorData CO2 { get; }
    }

    public interface IWallController : INetworkElement
    {


    }

    public interface IControllerElement : INetworkElement
    {
    }

    public interface IThermoElement : INetworkElement
    {
        Task SetLevel(byte newValue);
        Task SetTemperature(float newValue);

        SensorData TemperatureSetpoint { get; }
    }

    public interface IUnknownElement : INetworkElement
    {

    }

    public interface IDefectElement : INetworkElement
    {


    }

    public interface IGPSTracker :  INetworkElement
    {
        SensorData Battery { get; }
        LocatorPosition Position { get; }
        LocatorPosition[] PositionTrace { get; }
        DateTime LastMessageReceived { get; }
    }
}
