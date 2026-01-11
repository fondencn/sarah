using System.Threading.Tasks;
using Sarah.API.Interfaces.Services;

namespace Sarah.API.Interfaces
{
    public interface INetworkElement
    {
        byte NodeID { get; }
        string StateInfo { get; }
        string ClassDescription { get; }
        bool? IsActive { get; }

        Task<IAssociationGroup[]> GetAssociationGroups(IDeviceService deviceService);
        Task SetAssociationGroup(IDeviceService deviceService, byte groupId, byte[] newNodes);
    }
}
