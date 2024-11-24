using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
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

        public override Task InitializeAsync()
        {
            ReportEvent(new NetworkEvent<string>(this.NodeID, "Controller gestartet"));
            return Task.CompletedTask;
        }
    }
}