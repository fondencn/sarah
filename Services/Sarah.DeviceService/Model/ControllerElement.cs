using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    public class ControllerElement : NetworkElement, IControllerElement
    {
        public ControllerElement(byte nodeid) : base(nodeid)
        {
        }

        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            ReportEvent(new NetworkEvent<string>(this.NodeID, "Controller gestartet"));
            return Task.CompletedTask;
        }
    }
}