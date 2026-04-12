using System.Threading;
using System.Threading.Tasks;
using Sarah.API.BusinessObjects;


namespace Sarah.API.Interfaces
{
    public interface IEventProcessingService 
    {

        Task Start();
        Task SubscribeNetworkEventAsync(INetworkEventSubscriber subscriber, CancellationToken cancellationToken = default);
        Task SubscribeSpeechEventAsync(ISpeechEventSubscriber subscriber, CancellationToken cancellationToken = default);
        Task PublishNetworkEventAsync<T>(NetworkEvent<T> networkEvent, CancellationToken cancellationToken = default);
        Task PublishNetworkEventAsync(NetworkEvent networkEvent, CancellationToken cancellationToken = default);
        Task PublishAirQualityEventAsync(AirQualityChangedEvent airQualityChangedEvent, CancellationToken cancellationToken = default);
        Task PublishSay(SayEvent e, CancellationToken cancellationToken = default);
        Task PublishPersonAvailabilityAsync(PersonAvailabilityEvent personAvailabilityEvent, CancellationToken cancellationToken = default);
        Task PublishGeoFenceEventAsync(PersonGeoFenceEvent personGeoFenceEvent, CancellationToken cancellationToken = default);
        Task PublishWeatherWarningEventAsync(WeatherWarningEvent weatherWarningEvent, CancellationToken cancellationToken = default);
        Task PublishTimerEventAsync(TimerEvent timerEvent, CancellationToken cancellationToken = default);
        Task PublishStartPlayAudioEventAsync(StartAudioEvent startAudioEvent, CancellationToken cancellationToken = default);
        Task PublishStopPlayAudioEventAsync(StopAudioEvent stopAudioEvent, CancellationToken cancellationToken = default);
    }
}