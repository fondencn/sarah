using System.Threading;
using System.Threading.Tasks;
using Sarah.API.BusinessObjects;


namespace Sarah.API.Interfaces
{
    public interface IEventProcessingService 
    {
        Task PublishNetworkEventAsync(NetworkEvent networkEvent, CancellationToken cancellationToken = default);
    }
}