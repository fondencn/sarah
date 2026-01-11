using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sarah.DeviceService.Model.Animations
{
    public class BlinkAnimation : Animation
    {
        public TimeSpan OnTime { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan OffTime { get; set; } = TimeSpan.FromSeconds(3);

        public Color? Color { get; set; }

        private Color? PreviousColor { get; set; }

        public int BlinkCount { get; set; }

        public BlinkAnimation(ILamp lamp) : base(lamp)
        {

        }

        protected override async Task AnimationLoopInternal(CancellationToken cancellationToken)
        {
            if(this.Color.HasValue)
            {
                if (this.LampDevice.Color != "?")
                {
                    this.PreviousColor = ColorConverter.FromHex(this.LampDevice.Color);
                }
                await LampDevice.SetColor(ColorConverter.ToHex(this.Color.Value));
            }

            int numBlinks = 0;
            while(!cancellationToken.IsCancellationRequested || (numBlinks <= this.BlinkCount && this.BlinkCount > 0))
            {
                await this.LampDevice.SetBrightness(255);
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(this.OnTime);
                if (cancellationToken.IsCancellationRequested) break;
                await this.LampDevice.SetBrightness(0);
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(this.OffTime);
                numBlinks++;
            }

            if (this.PreviousColor.HasValue)
            {
                await this.LampDevice.SetColor(ColorConverter.ToHex(this.PreviousColor.Value));
            }
        }
    }
}
