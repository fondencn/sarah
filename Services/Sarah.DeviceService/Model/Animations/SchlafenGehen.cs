using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sarah.DeviceService.Model.Animations
{
    [SceneName("Schlafen gehen")]
    public class SchlafenGehen : Scene
    {
        protected override async void Start()
        {
            Lamp arbeitsZimmerLedStrip = InteLukNetwork.Instance.GetNetworkItem(14) as Lamp;
            Lamp mamaZimmerNachtLampe = InteLukNetwork.Instance.GetNetworkItem(21) as Lamp;
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
