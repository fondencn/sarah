using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System.Threading.Tasks;
using ZWave;

namespace Sarah.DeviceService.Model
{
    public class DefectElement : NetworkElement, IDefectElement
    {
        private string _genericType = null;
        public override string ClassDescription => this._genericType;


        public DefectElement(byte nodeid) : base(nodeid)
        {

        }

        public override async Task InitializeAsync()
        {
            Node node = InteLukNetwork.Instance.GetNodeInternal(this.NodeID);

            if (node != null)
            {
                var proto = await node.GetProtocolInfo();
                this._genericType = proto.GenericType.ToString();
            }
        }
    }
}

