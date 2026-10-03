using Sarah.API.BusinessObjects;
using Sarah.Rules.Conditions;

namespace Sarah.Rules.Tests;

public class DoorSensorConditionTests
{
    [Fact]
    public void Evaluate_ReturnsTrue_ForOpenedAlert_WhenConfiguredOpen()
    {
        var condition = new DoorSensorCondition(targetNodeId: 34)
        {
            Value = DoorSensorState.Offen
        };

        var evt = new DoorOrWindowOpenedEvent(
            source: 34,
            isWindow: false,
            deviceName: "Haustuer",
            deviceRoom: "Flur",
            turnedOffHeatings: new List<string>());

        var result = condition.Evaluate(evt);

        Assert.True(result);
    }

    [Fact]
    public void Evaluate_ReturnsTrue_ForClosedAlert_WhenConfiguredClosed()
    {
        var condition = new DoorSensorCondition(targetNodeId: 34)
        {
            Value = DoorSensorState.Geschlossen
        };

        var evt = new DoorOrWindowClosedEvent(
            source: 34,
            isWindow: false,
            deviceName: "Haustuer",
            deviceRoom: "Flur",
            turnedOnHeatings: new List<string>(),
            openedDuration: TimeSpan.FromMinutes(3));

        var result = condition.Evaluate(evt);

        Assert.True(result);
    }

    [Fact]
    public void Evaluate_ReturnsFalse_ForStillOpenAlert()
    {
        var condition = new DoorSensorCondition(targetNodeId: 34)
        {
            Value = DoorSensorState.Offen
        };

        var evt = new DoorOrWindowStillOpenEvent(
            source: 34,
            isWindow: false,
            deviceName: "Haustuer",
            deviceRoom: "Flur",
            openedSince: TimeSpan.FromMinutes(10));

        var result = condition.Evaluate(evt);

        Assert.False(result);
    }
}
