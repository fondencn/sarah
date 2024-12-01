using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.DeviceService.Model.Animations
{
    [SceneName("Sonnenaufgang")]
    public class Sunrise : Scene
    {
        public Sunrise(IDeviceService deviceService) : base(deviceService)
        {
        }

        protected override void Start()
        {
            // Das läuft asynchron weil es echt lang dauert
            Task.Run(async () =>
            {
                int waitTimeMs = 10000;
                Lamp wohnzimmerLampe = _deviceService.GetNetworkItem(24) as Lamp;
                await wohnzimmerLampe.SetColor("#402200");
                await wohnzimmerLampe.SetBrightness(255);
                await Task.Delay(waitTimeMs);
                await wohnzimmerLampe.SetColor("#733e02");
                await Task.Delay(waitTimeMs);
                await wohnzimmerLampe.SetColor("#ad5d02");
                await Task.Delay(waitTimeMs);
                await wohnzimmerLampe.SetColor("#ff8903");
                await Task.Delay(waitTimeMs);
                await wohnzimmerLampe.SetColor("#ffbc03");
                await Task.Delay(waitTimeMs);
                await wohnzimmerLampe.SetColor("#ffffff");
                await Task.Delay(waitTimeMs);
                await wohnzimmerLampe.SetWarmWhite();
                await Task.Delay(waitTimeMs);
                Scene.Stop(this.GetType());
            });
        }

        protected override void Stop()
        {
        }
    }
}
