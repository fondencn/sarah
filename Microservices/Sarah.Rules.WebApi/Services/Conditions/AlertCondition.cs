using System.Linq;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{


    /// <summary>
    /// Bedingung, wenn ein Gerät einen Alarm meldet
    /// </summary>
    public class AlertCondition : RuleCondition
    {
        private readonly IDeviceService _devices;
        public AlertCondition(byte targetNodeId, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
        }


        /// <summary>
        /// Wertet den aktuellen Türsensor Zustand aus und gibt true zurück, wenn dieser dem Wert von Value entspricht.
        /// </summary>
        /// <returns></returns>
        public override bool Evaluate(NetworkEvent evt)
        {
            ISmokeSensor sensor = _devices.SmokeSensors.FirstOrDefault(item => item.NodeID == this.TargetNodeId);

            float? val = sensor?.Alarm?.Value;

            return val != null && val.HasValue && val.Value > 0;
        }

    }

}
