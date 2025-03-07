using System.Linq;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{


    /// <summary>
    /// Bedingung, die den Zustand eines Türsensors auswertet.
    /// </summary>
    public class DoorSensorCondition : RuleCondition
    {
        private readonly IDeviceService _devices;

        public DoorSensorCondition(byte targetNodeId, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
        }

        /// <summary>
        /// Sensorstatus, der eingestellt werden muss, damit diese Condition zutrifft. 
        /// </summary>
        public DoorSensorState Value { get; set; }

        /// <summary>
        /// Wertet den aktuellen Türsensor Zustand aus und gibt true zurück, wenn dieser dem Wert von Value entspricht.
        /// </summary>
        /// <returns></returns>
        public override bool Evaluate(NetworkEvent evt)
        {
            IDoorSensor sensor = _devices.DoorSensors.FirstOrDefault(item => item.NodeID == this.TargetNodeId);

            return Value == sensor?.State;
        }

    }

}
