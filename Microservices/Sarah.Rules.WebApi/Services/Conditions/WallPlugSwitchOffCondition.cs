using System.Linq;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    /// <summary>
    /// Bedingung, die Eintritt, wenn eine Steckdose abgeschaltet wird
    /// </summary>
    public class WallPlugSwitchOffCondition : RuleCondition
    {
        private readonly IDeviceService _devices;
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="targetNodeId"></param>
        public WallPlugSwitchOffCondition(byte targetNodeId, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
        }


        /// <summary>
        /// wenn eine Steckdose abgeschaltet wird
        /// </summary>
        /// <param name="sourceNodeId"></param>
        /// <returns></returns>
        public override bool Evaluate(NetworkEvent evt)
        {
            IWallPlug? wallplug = this._devices.WallPlugs.FirstOrDefault(item => item.NodeID == this.TargetNodeId);
            return wallplug?.IsOn == false;
        }
    }
}
