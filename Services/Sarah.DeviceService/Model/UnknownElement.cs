using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System.Threading.Tasks;
using Sarah.API.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Sarah.DeviceService.Model
{
    public class UnknownElement : NetworkElement, IUnknownElement
    {
        private string _genericType = null;
        public override string ClassDescription => this._genericType;


        public UnknownElement(byte nodeid) : base(nodeid)
        {
        }

        public override async Task InitializeAsync(IDeviceService deviceService, IConfiguration config = null)
        {
            INode node = deviceService.GetNode(this.NodeID);
            this._genericType = await node?.GetDeviceTypeName();
        }
    }
}
