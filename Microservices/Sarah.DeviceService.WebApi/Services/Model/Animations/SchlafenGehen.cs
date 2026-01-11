using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sarah.API.Interfaces.Services;

namespace Sarah.DeviceService.Model.Animations
{
    [SceneName("Schlafen gehen")]
    public class SchlafenGehen : Scene
    {
        public SchlafenGehen(IDeviceService deviceService) : base(deviceService)
        {
        }

        protected override async void Start()
        {
            Lamp arbeitsZimmerLedStrip = _deviceService.GetNetworkItem(14) as Lamp;
            Lamp mamaZimmerNachtLampe = _deviceService.GetNetworkItem(21) as Lamp;
            await mamaZimmerNachtLampe.SetWarmWhite(); 
            await mamaZimmerNachtLampe.SetBrightness(255);
            await arbeitsZimmerLedStrip.SetBrightness(0);
            await Task.Delay(2000);
            Scene.Stop(this.GetType());
        }

        protected override void Stop()
        {
        }
    }
}
