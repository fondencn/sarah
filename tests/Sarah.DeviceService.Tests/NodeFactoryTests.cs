using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.DeviceService.Model;
using Sarah.DeviceService.WebApi.Extensions;
using Sarah.DeviceService.WebApi.Services;
using Sarah.Messaging.RabbitMQ;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class NodeFactoryTests
{
    [Fact]
    public void CreateByNodeId_ControllerNode_ReturnsControllerElement()
    {
        var factory = CreateFactory();

        var result = factory.CreateByNodeId(1);

        Assert.IsType<ControllerElement>(result);
    }

    [Fact]
    public void CreateByNodeId_WallPlugNode_ReturnsWallPlug()
    {
        var factory = CreateFactory();

        var result = factory.CreateByNodeId(10);

        Assert.IsType<ZWaveWallPlug>(result);
    }

    private static NodeFactory CreateFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var rabbitMqClient = new RabbitMQClient(NullLogger<RabbitMQClient>.Instance, configuration);
        var publisher = new NetworkElementPublisher(rabbitMqClient);

        return new NodeFactory(
            new FakeEventProcessingService(),
            NullLogger<NodeFactory>.Instance,
            publisher,
            configuration);
    }

    private sealed class FakeEventProcessingService : IEventProcessingService
    {
        public Task Start() => Task.CompletedTask;

        public Task SubscribeNetworkEventAsync(INetworkEventSubscriber subscriber, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SubscribeSpeechEventAsync(ISpeechEventSubscriber subscriber, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishNetworkEventAsync<T>(NetworkEvent<T> networkEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishNetworkEventAsync(NetworkEvent networkEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishAirQualityEventAsync(AirQualityChangedEvent airQualityChangedEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishSay(SayEvent e, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishPersonAvailabilityAsync(PersonAvailabilityEvent personAvailabilityEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishGeoFenceEventAsync(PersonGeoFenceEvent personGeoFenceEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishWeatherWarningEventAsync(WeatherWarningEvent weatherWarningEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishTimerEventAsync(TimerEvent timerEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishStartPlayAudioEventAsync(StartAudioEvent startAudioEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishStopPlayAudioEventAsync(StopAudioEvent stopAudioEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}