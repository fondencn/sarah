using Sarah.API.BusinessObjects;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces.Services
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
}
