#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.DeviceService.Model;
using Sarah.DeviceService.WebApi.Extensions;
using Sarah.Messaging.RabbitMQ;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class WallPlugStateChangedBehaviorTests
{
    [Fact]
    public void MeterW_Changed_UpdatesMeterValue()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        wallPlug.ApplyMeter(5f);

        Assert.Equal(5f, wallPlug.Meter_W.Value);
    }

    [Fact]
    public void MeterW_Unchanged_DoesNotRepublishStateEvent()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        wallPlug.ApplyMeter(5f);
        wallPlug.ApplyMeter(5f);

        Assert.Equal(5f, wallPlug.Meter_W.Value);
    }

    [Fact]
    public void IsOn_Unchanged_DoesNotPublishIsOnEvent()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        wallPlug.ApplyIsOn(false);
        wallPlug.ApplyIsOn(false);

        Assert.Equal(DateTime.MinValue, wallPlug.LastStateChange);
    }

    [Fact]
    public void LastChangeToPowerHigh_RemainsUnchanged_BelowThreshold()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        Assert.Equal(DateTime.MinValue, wallPlug.LastChangeToPowerHigh);

        wallPlug.ApplyMeter(5f);
        wallPlug.ApplyMeter(6f);

        Assert.Equal(DateTime.MinValue, wallPlug.LastChangeToPowerHigh);
    }

    private sealed class TestWallPlug : WallPlug
    {
        public TestWallPlug(byte nodeId, NetworkElementPublisher publisher)
            : base(nodeId, publisher, NullLogger.Instance)
        {
        }

        public override Task SetState(bool newState)
        {
            IsOn = newState;
            return Task.CompletedTask;
        }

        public void ApplyIsOn(bool value) => IsOn = value;

        public void ApplyMeter(float watts) => Meter_W = new SensorData(watts, "W");
    }

    private sealed class RecordingPublisher : NetworkElementPublisher
    {
        public RecordingPublisher()
            : base(
                new RabbitMQClient(
                    NullLogger<RabbitMQClient>.Instance,
                    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build()))
        {
        }
    }
}
