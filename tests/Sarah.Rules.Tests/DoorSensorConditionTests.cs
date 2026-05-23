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

        var evt = new DoorMonitorAlertEvent(
            sourceNodeId: 34,
            deviceName: "Haustuer",
            isWindow: false,
            alertType: DoorMonitorAlertType.Opened,
            openDurationMinutes: 0,
            wasOpenLongEnough: false,
            roomTemperature: null,
            heatingDifferentials: null,
            nextAlertIntervalMinutes: null,
            isLoud: false);

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

        var evt = new DoorMonitorAlertEvent(
            sourceNodeId: 34,
            deviceName: "Haustuer",
            isWindow: false,
            alertType: DoorMonitorAlertType.Closed,
            openDurationMinutes: 3,
            wasOpenLongEnough: true,
            roomTemperature: 20.5f,
            heatingDifferentials: null,
            nextAlertIntervalMinutes: null,
            isLoud: false);

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

        var evt = new DoorMonitorAlertEvent(
            sourceNodeId: 34,
            deviceName: "Haustuer",
            isWindow: false,
            alertType: DoorMonitorAlertType.StillOpen,
            openDurationMinutes: 10,
            wasOpenLongEnough: true,
            roomTemperature: 19.0f,
            heatingDifferentials: null,
            nextAlertIntervalMinutes: 15,
            isLoud: false);

        var result = condition.Evaluate(evt);

        Assert.False(result);
    }
}
