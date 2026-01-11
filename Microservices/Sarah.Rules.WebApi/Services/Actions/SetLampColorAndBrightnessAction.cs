using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
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
        private readonly ILogger _logger;


        public SetLampColorAndBrightnessAction(byte targetNodeId, byte brightness, string color, IDeviceService devices, ILogger logger) : base()
        {
            this._devices = devices;
            this.TargetNodeId = targetNodeId;
            this.Brightness = brightness;
            this.Color = color;
            this._logger = logger;
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
                _logger.LogDebug("EXCEPTION in SetLampColorAndBrightnessAction::Execute: {ErrorMessage}", ex.Message);
            }
        }
    }
}
