using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Tests;

public class DoorAndHeatingMessageContractTests
{
    [Fact]
    public void DoorSensorStateChangedMessage_UsesNewDoorStateChangedTopicAndTransitionData()
    {
        var changedAt = new DateTime(2026, 5, 21, 5, 0, 0, DateTimeKind.Utc);

        var message = new DoorSensorStateChangedMessage(34, Sarah.Messaging.RabbitMQ.Messages.DoorSensorStateValue.Open, changedAt);

        Assert.Equal(MessageTopics.NetworkEventsDoorState, message.Topic);
        Assert.Equal("network.events.door.state.changed", message.Topic);
        Assert.Equal(34, message.SourceNodeId);
        Assert.Equal(Sarah.Messaging.RabbitMQ.Messages.DoorSensorStateValue.Open, message.State);
        Assert.Equal(changedAt, message.ChangedAtUtc);
        Assert.True(message.IsOpen);
    }

    [Fact]
    public void DoorSensorStateChangedEvent_CarriesTypedStateAndTimestamp()
    {
        var changedAt = new DateTime(2026, 5, 21, 5, 10, 0, DateTimeKind.Utc);
        var evt = new DoorSensorStateChangedEvent(34, Sarah.API.BusinessObjects.DoorSensorStateValue.Closed, changedAt);

        Assert.Equal(34, evt.SourceNodeId);
        Assert.Equal(Sarah.API.BusinessObjects.DoorSensorStateValue.Closed, evt.State);
        Assert.False(evt.IsOpen);
        Assert.Equal(changedAt, evt.ChangedAtUtc);
    }

    [Fact]
    public void DoorMonitorAlertEvent_CarriesHeatingDifferentials()
    {
        var differential = new DoorMonitorHeatingDifferential(
            roomName: "Wohnzimmer",
            previousTemperature: 21.5f,
            currentTemperature: 12f,
            temperatureDelta: -9.5f,
            changeType: DoorMonitorHeatingDifferentialType.TurnedOff);

        var evt = new DoorMonitorAlertEvent(
            sourceNodeId: 22,
            deviceName: "Fenster Wohnzimmer",
            isWindow: true,
            alertType: DoorMonitorAlertType.StillOpen,
            openDurationMinutes: 10,
            wasOpenLongEnough: true,
            roomTemperature: 20.1f,
            heatingDifferentials: new[] { differential },
            nextAlertIntervalMinutes: 15,
            isLoud: false);

        Assert.Single(evt.HeatingDifferentials!);
        Assert.Equal("Wohnzimmer", evt.HeatingDifferentials![0].RoomName);
        Assert.Equal(21.5f, evt.HeatingDifferentials[0].PreviousTemperature);
        Assert.Equal(12f, evt.HeatingDifferentials[0].CurrentTemperature);
        Assert.Equal(-9.5f, evt.HeatingDifferentials[0].TemperatureDelta);
        Assert.Equal(DoorMonitorHeatingDifferentialType.TurnedOff, evt.HeatingDifferentials[0].ChangeType);
    }
}
