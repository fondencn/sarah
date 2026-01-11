using System;
using System.Linq;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Conditions
{
    public class WallPlugPowerIncreasingCondition : RuleCondition
    {
        private readonly IDeviceService _devices;

        public WallPlugPowerIncreasingCondition(byte targetNodeId, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            IWallPlug wallplug = this._devices.WallPlugs.FirstOrDefault(item => item.NodeID == this.TargetNodeId);

            return wallplug.IsOn && (DateTime.Now - wallplug.LastIncreasePower).TotalSeconds < 60;
        }
    }
    public class WallPlugPowerDecreasingCondition : RuleCondition
    {
        private readonly IDeviceService _devices;
        public WallPlugPowerDecreasingCondition(byte targetNodeId, IDeviceService devices) : base(targetNodeId)
        {
            this._devices = devices;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            IWallPlug wallplug = this._devices.WallPlugs.FirstOrDefault(item => item.NodeID == this.TargetNodeId);

            return wallplug.IsOn && (DateTime.Now - wallplug.LastDecreasePower).TotalSeconds < 60;
        }
    }
}
