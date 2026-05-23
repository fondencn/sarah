using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.DeviceService.Model;
using Sarah.DeviceService.WebApi.Extensions;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class WallPlugStateChangedBehaviorTests
{
    [Fact]
    public void MeterW_Changed_PublishesStateEventWithCurrentWattage()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        wallPlug.ApplyMeter(42.5f);

        var message = Assert.Single(publisher.StateChangedMessages);
        Assert.Equal(42.5f, message.CurrentWattage);
    }

    [Fact]
    public void MeterW_Unchanged_DoesNotRepublishStateEvent()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        wallPlug.ApplyMeter(15f);
        wallPlug.ApplyMeter(15f);

        Assert.Single(publisher.StateChangedMessages);
    }

    [Fact]
    public void IsOn_Unchanged_DoesNotRepublishStateEvent()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        wallPlug.ApplyIsOn(true);
        wallPlug.ApplyIsOn(true);

        Assert.Single(publisher.StateChangedMessages);
    }

    [Fact]
    public void LastChangeToPowerHigh_UpdatesOnlyOnZeroToClearNonZeroTransition()
    {
        var publisher = new RecordingPublisher();
        var wallPlug = new TestWallPlug(10, publisher);

        Assert.Equal(DateTime.MinValue, wallPlug.LastChangeToPowerHigh);

        wallPlug.ApplyMeter(25f);
        var afterFirstTransition = wallPlug.LastChangeToPowerHigh;
        Assert.NotEqual(DateTime.MinValue, afterFirstTransition);

        wallPlug.ApplyMeter(40f);
        Assert.Equal(afterFirstTransition, wallPlug.LastChangeToPowerHigh);

        wallPlug.ApplyMeter(0f);
        wallPlug.ApplyMeter(30f);

        Assert.True(wallPlug.LastChangeToPowerHigh >= afterFirstTransition);
        Assert.Equal(4, publisher.StateChangedMessages.Count);
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
        public List<WallPlugStateChangedMessage> StateChangedMessages { get; } = new();

        public RecordingPublisher()
            : base(
                new RabbitMQClient(
                    NullLogger<RabbitMQClient>.Instance,
                    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build()))
        {
        }

        public override Task ReportWallPlugStateChanged(
            NetworkElement element,
            bool isOn,
            float currentWattage,
            DateTime lastChangeToPowerLow,
            DateTime lastChangeToPowerHigh)
        {
            StateChangedMessages.Add(new WallPlugStateChangedMessage(
                element.NodeID,
                isOn,
                currentWattage,
                lastChangeToPowerLow,
                lastChangeToPowerHigh));
            return Task.CompletedTask;
        }
    }
}
