using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System.Threading.Tasks;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Sarah.DeviceService.WebApi.Extensions;

namespace Sarah.DeviceService.Model
{
    public class UnknownElement : NetworkElement, IUnknownElement
    {
        private string? _genericType = null;
        public override string ClassDescription => this._genericType ?? "Unknown";


        public UnknownElement(byte nodeid, NetworkElementPublisher publisher, ILogger logger) : base(nodeid, logger)
        {
        }

        public override async Task InitializeAsync(IDeviceService deviceService, IConfiguration config)
        {
            INode? node = deviceService.GetNode(this.NodeID);
            if (node != null)
            {
                this._genericType = await node.GetDeviceTypeName() ?? "Unknown";
            }
        }
    }
}
