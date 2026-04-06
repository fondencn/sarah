using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.ServiceClients;
using System;

namespace Sarah.Rules.Actions
{
    public class SetLampColorAndBrightnessAction : RuleAction
    {
        private readonly DeviceServiceClient _deviceServiceClient;
        private byte TargetNodeId { get; set; }
        private byte Brightness { get; set; }
        private string Color { get; set; }
        private readonly ILogger _logger;

        public SetLampColorAndBrightnessAction(byte targetNodeId, byte brightness, string color, DeviceServiceClient deviceServiceClient, ILogger logger) : base()
        {
            this._deviceServiceClient = deviceServiceClient;
            this.TargetNodeId = targetNodeId;
            this.Brightness = brightness;
            this.Color = color;
            this._logger = logger;
        }

        public override async void Execute(NetworkEvent sourceEvent)
        {
            try
            {
                // Fetch live lamp state for debounce check
                DeviceDto? liveDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync(this.TargetNodeId);
                if (liveDevice?.Lamp == null
                    || !liveDevice.Lamp.LastChange.HasValue
                    || (DateTime.Now - liveDevice.Lamp.LastChange.GetValueOrDefault()).TotalSeconds > 10)
                {
                    await _deviceServiceClient.SetLampColorAndBrightnessByNodeAsync(this.TargetNodeId, this.Color, this.Brightness);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("EXCEPTION in SetLampColorAndBrightnessAction::Execute: {ErrorMessage}", ex.Message);
            }
        }
    }
}
