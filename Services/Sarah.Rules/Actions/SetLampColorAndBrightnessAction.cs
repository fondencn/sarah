using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using Sarah.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sarah.Rules.Actions
{
    public class SetLampColorAndBrightnessAction : RuleAction
    {
        private readonly IDeviceService _devices;
        private byte TargetNodeId { get; set; }
        private byte Brightness { get; set; }
        private string Color { get; set; }


        public SetLampColorAndBrightnessAction(byte targetNodeId, byte brightness, string color, IDeviceService devices) : base()
        {
            this._devices = devices;
            this.TargetNodeId = targetNodeId;
            this.Brightness = brightness;
            this.Color = color;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {

            try
            {
                var lamp = this._devices.Lamps.First(item => item.NodeID == TargetNodeId);
                if (lamp != null && lamp.LastChange.HasValue && (DateTime.Now - lamp.LastChange.GetValueOrDefault()).TotalSeconds > 10)
                {
                    await lamp.SetBrightness(this.Brightness);
                    if(!String.IsNullOrEmpty(this.Color))
                    {
                        await lamp.SetColor(this.Color);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogDebug("EXCEPTION in SetLampColorAndBrightnessAction::Execute: " + ex.Message);
            }
        }
    }
}
