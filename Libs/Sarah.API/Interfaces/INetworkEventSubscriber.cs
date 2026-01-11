using Sarah.API.BusinessObjects;
using System.Threading.Tasks;

namespace Sarah.API.Interfaces
{
    public interface INetworkEventSubscriber
    {
        Task Notify(NetworkEvent e);
    }
}
