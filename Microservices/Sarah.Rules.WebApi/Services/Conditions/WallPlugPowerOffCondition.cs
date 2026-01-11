using System;
using System.Linq;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    /// <summary>
    /// Bedingung, die Eintritt, wenn die Leistung einer Steckdose gegen 0 geht, diese aber mehr als 1 Minute eingeschaltet wurde
    /// </summary>
    public class WallPlugPowerOffCondition : RuleCondition
    {
        private readonly IDeviceService _devices;
        
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="targetNodeId"></param>
        public WallPlugPowerOffCondition(byte targetNodeId, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
        }

        /// <summary>
        /// Tritt ein, wenn die an der Steckjdose anliegende Leistung zuletzt in weniger als 60 Sekunden unter 1W gesunken ist
        /// </summary>
        /// <param name="sourceNodeId"></param>
        /// <returns></returns>
        public override bool Evaluate(NetworkEvent evt)
        {
            IWallPlug wallplug = this._devices.WallPlugs.FirstOrDefault(item => item.NodeID == this.TargetNodeId);

            return wallplug.IsOn && (DateTime.Now -  wallplug.LastChangeToPowerLow).TotalSeconds < 60;
        }
    }
}
