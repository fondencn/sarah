using System.Threading;
using System.Threading.Tasks;
using Sarah.API.BusinessObjects;


namespace Sarah.API.Interfaces
{
    public interface IEventProcessingService 
    {

        Task Start();
        Task PublishNetworkEventAsync<T>(NetworkEvent<T> networkEvent, CancellationToken cancellationToken = default);
        Task PublishNetworkEventAsync(NetworkEvent networkEvent, CancellationToken cancellationToken = default);
        Task SubscribeNetworkEventAsync(INetworkEventSubscriber subscriber, CancellationToken cancellationToken = default);
        Task PublishAirQualityEventAsync(AirQualityChangedEvent airQualityChangedEvent, CancellationToken cancellationToken = default);
        Task PublishSay(SayEvent e, CancellationToken cancellationToken = default);
    }
}