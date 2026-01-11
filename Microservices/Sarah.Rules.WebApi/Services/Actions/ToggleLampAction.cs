using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sarah.Rules.Actions
{
    public class ToggleLampAction : RuleAction
    {
        private readonly IDeviceService _devices;
        private byte TargetNodeId { get; set; }
        public ToggleLampAction(byte targetNodeId, IDeviceService devices)
        {
            this._devices = devices;
            this.TargetNodeId = targetNodeId;
        }

        public override void Execute(NetworkEvent sourceEvent)
        {
             _ = _devices.Lamps.First(item => item.NodeID == this.TargetNodeId).ToggleState();
        }
    }
}
