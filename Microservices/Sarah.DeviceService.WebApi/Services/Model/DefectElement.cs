using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.Model.Extensions;
using System.Threading.Tasks;
using ZWave;
using Sarah.DeviceService.WebApi.Extensions;

namespace Sarah.DeviceService.Model
{
    public class DefectElement : NetworkElement, IDefectElement
    {
        private string _genericType = null!;
        public override string ClassDescription => this._genericType;


        public DefectElement(byte nodeid, NetworkElementPublisher publisher, ILogger<DefectElement>? logger = null) : base(nodeid, logger)
        {

        }

        public override async Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            Node? node = deviceService.GetZWaveNode(this.NodeID);

            if (node != null)
            {
                var proto = await node.GetProtocolInfo();
                this._genericType = proto.GenericType.ToString();
            }
        }
    }
}

