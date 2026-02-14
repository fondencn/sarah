using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Sarah.DeviceService.Model.Animations
{
    [SceneName("Roter Alarm")]
    [SceneName("Alarmstufe rot")]
    public class RedAlert : Scene, IDisposable
    {
        private readonly List<Animation> _runningAnimations = new List<Animation>();
        private bool _disposed = false;

        public RedAlert(IDeviceService deviceService) : base(deviceService)
        {
        }

        protected override void Start()
        {
            foreach (Lamp lamp in _deviceService.Lamps.Where(item => item.NodeID != 3)) //3 ist die versteckte Glühbirne, die nicht nehmen, sonst Timeouts
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
            if (_disposed)
                return;
                
            foreach(Animation anim in _runningAnimations)
            {
                anim.Stop();
                anim.Dispose();
            }
            _runningAnimations.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop only if not already disposed
                    foreach(Animation anim in _runningAnimations.ToList())
                    {
                        try
                        {
                            anim.Stop();
                            anim.Dispose();
                        }
                        catch
                        {
                            // Ignore disposal errors to allow cleanup to continue
                        }
                    }
                    _runningAnimations.Clear();
                }
                _disposed = true;
            }
        }
    }
}
