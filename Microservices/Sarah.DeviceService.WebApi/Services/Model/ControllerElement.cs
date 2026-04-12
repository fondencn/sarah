using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.WebApi.Extensions;
using System.Threading.Tasks;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    public class ControllerElement : NetworkElement, IControllerElement
    {
        private readonly NetworkElementPublisher _publisher;
        
        public ControllerElement(byte nodeid, NetworkElementPublisher publisher, ILogger<ControllerElement> logger) : base(nodeid, logger)
        {
            _publisher = publisher;
        }

        public override Task InitializeAsync(IDeviceService deviceService, IConfiguration config)
        {
            _ = _publisher.ReportEvent(this, "Status", "Controller gestartet");
            return Task.CompletedTask;
        }
    }
}