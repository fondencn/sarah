using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using System.Threading.Tasks;
using ZWave;

namespace Sarah.DeviceService.Model
{
    public class DefectElement : NetworkElement, IDefectElement
    {
        private string _genericType = null;
        public override string ClassDescription => this._genericType;


        public DefectElement(byte nodeid, IEventProcessingService events) : base(nodeid, events)
        {

        }

        public override async Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            Node node = deviceService.GetNode(this.NodeID) as Node;

            if (node != null)
            {
                var proto = await node.GetProtocolInfo();
                this._genericType = proto.GenericType.ToString();
            }
        }
    }
}

