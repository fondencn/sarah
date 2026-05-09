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
            if (evt is DoorMonitorAlertEvent doorEvt)
            {
                bool? isOpen = doorEvt.AlertType switch
                {
                    DoorMonitorAlertType.Opened => true,
                    DoorMonitorAlertType.Closed => false,
                    _ => null
                };

                if (!isOpen.HasValue)
                {
                    return false;
                }

                return Value == (isOpen.Value ? DoorSensorState.Offen : DoorSensorState.Geschlossen);
            }
            return false;
        }
    }
}
