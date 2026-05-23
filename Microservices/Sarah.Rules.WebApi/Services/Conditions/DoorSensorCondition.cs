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
        public DoorSensorCondition(byte targetNodeId) : base(targetNodeId)
        {
        }

        /// <summary>
        /// Sensorstatus, der eingestellt werden muss, damit diese Condition zutrifft. 
        /// </summary>
        public DoorSensorState Value { get; set; }

        /// <summary>
        /// Wertet den aktuellen Türsensor Zustand aus und gibt true zurück, wenn dieser dem Wert von Value entspricht.
        /// </summary>
        public override bool Evaluate(NetworkEvent evt)
        {
            if (evt is DoorOrWindowOpenedEvent && Value == DoorSensorState.Offen)
                return TargetNodeId == 0 || evt.SourceNodeId == TargetNodeId;

            if (evt is DoorOrWindowClosedEvent && Value == DoorSensorState.Geschlossen)
                return TargetNodeId == 0 || evt.SourceNodeId == TargetNodeId;

            return false;
        }
    }
}
