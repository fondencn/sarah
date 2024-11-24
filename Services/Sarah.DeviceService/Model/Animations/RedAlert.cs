using Sarah.API.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Sarah.DeviceService.Model.Animations
{
    [SceneName("Roter Alarm")]
    [SceneName("Alarmstufe rot")]
    public class RedAlert : Scene
    {
        private readonly List<Animation> _runningAnimations = new List<Animation>();


        protected override void Start()
        {
            foreach (Lamp lamp in InteLukNetwork.Instance.Lamps.Where(item => item.NodeID != 3)) //3 ist die versteckte Glühbirne, die nicht nehmen, sonst Timeouts
            {
                BlinkAnimation anim = new BlinkAnimation(lamp)
                {
                    Color = Color.Red
                };
                anim.Start();
                _runningAnimations.Add(anim);
            }
        }

        protected override void Stop()
        {
            foreach(Animation anim in _runningAnimations)
            {
                anim.Stop();
                anim.Dispose();
            }
            _runningAnimations.Clear();
        }
    }
}
