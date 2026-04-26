using Sarah.API.BusinessObjects;
using Sarah.Rules;

namespace Sarah.Rules.Tests;

/// <summary>
/// Unit tests for the speech text generation logic in MonitoringRuleStore.
/// </summary>
public class MonitoringRuleStoreTextTests
{
    // ─── BatteryWarningEvent ────────────────────────────────────────────────────

    [Fact]
    public void BatteryWarning_SingleEmptyDevice_ReturnsEmptyBattery()
    {
        var evt = new BatteryWarningEvent(new[]
        {
            new BatteryDeviceInfo("Türsensor Flur", 3f)
        });

        string text = MonitoringRuleStore.BuildBatteryWarningText(evt);

        Assert.Contains("Türsensor Flur", text);
        Assert.Contains("Batterie leer", text);
        Assert.Contains("Achtung", text);
    }

    [Fact]
    public void BatteryWarning_SingleLowDevice_ReturnsPercentageWarning()
    {
        var evt = new BatteryWarningEvent(new[]
        {
            new BatteryDeviceInfo("Bewegungsmelder Bad", 8f)
        });

        string text = MonitoringRuleStore.BuildBatteryWarningText(evt);

        Assert.Contains("Bewegungsmelder Bad", text);
        Assert.Contains("Prozent", text);
    }

    [Fact]
    public void BatteryWarning_MultipleDevices_ContainsAllDeviceNames()
    {
        var evt = new BatteryWarningEvent(new[]
        {
            new BatteryDeviceInfo("Gerät A", 3f),
            new BatteryDeviceInfo("Gerät B", 9f)
        });

        string text = MonitoringRuleStore.BuildBatteryWarningText(evt);

        Assert.Contains("Gerät A", text);
        Assert.Contains("Gerät B", text);
    }

    // ─── DoorMonitorAlertEvent – Opened ────────────────────────────────────────

    [Fact]
    public void DoorAlert_Opened_Window_ContainsDasAndDeviceName()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster Wohnzimmer", true, DoorMonitorAlertType.Opened,
            0, false, null, null, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.StartsWith("Das Fenster Wohnzimmer wurde geöffnet.", text);
    }

    [Fact]
    public void DoorAlert_Opened_Door_ContainsDieAndDeviceName()
    {
        var evt = new DoorMonitorAlertEvent(34, "Haustür", false, DoorMonitorAlertType.Opened,
            0, false, null, null, null, null, true);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.StartsWith("Die Haustür wurde geöffnet.", text);
    }

    // ─── DoorMonitorAlertEvent – StillOpen ─────────────────────────────────────

    [Fact]
    public void DoorAlert_StillOpen_ZeroMinutes_ReturnsSeitKurzem()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster Wohnzimmer", true, DoorMonitorAlertType.StillOpen,
            0, false, null, null, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("seit kurzem offen", text);
    }

    [Fact]
    public void DoorAlert_StillOpen_OneMinute_ReturnsEineMinute()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster Wohnzimmer", true, DoorMonitorAlertType.StillOpen,
            1, false, null, null, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("eine Minute offen", text);
    }

    [Fact]
    public void DoorAlert_StillOpen_MultipleMinutes_ReturnsMinuteCount()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster Wohnzimmer", true, DoorMonitorAlertType.StillOpen,
            15, false, null, null, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("15 Minuten offen", text);
    }

    [Fact]
    public void DoorAlert_StillOpen_NextAlert60Min_ContainsEinerStunde()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster", true, DoorMonitorAlertType.StillOpen,
            60, false, null, null, null, 60, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("einer Stunde", text);
    }

    [Fact]
    public void DoorAlert_StillOpen_NextAlert15Min_Contains15Minuten()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster", true, DoorMonitorAlertType.StillOpen,
            15, false, null, null, null, 15, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("15 Minuten", text);
    }

    [Fact]
    public void DoorAlert_StillOpen_LowRoomTemp_ContainsTempAndNurNoch()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster", true, DoorMonitorAlertType.StillOpen,
            10, false, 15.5f, null, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("nur noch", text);
        Assert.Contains("15", text); // contains the temperature value
        Assert.Contains("Grad", text);
    }

    [Fact]
    public void DoorAlert_StillOpen_HeatingTurnedOff_ContainsRoomName()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster Schlafzimmer", true, DoorMonitorAlertType.StillOpen,
            5, false, null, new[] { "Schlafzimmer" }, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("Schlafzimmer", text);
        Assert.Contains("ausgeschalt", text);
    }

    // ─── DoorMonitorAlertEvent – Closed ────────────────────────────────────────

    [Fact]
    public void DoorAlert_Closed_WasOpenLongEnough_ContainsGeschlossen()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster", true, DoorMonitorAlertType.Closed,
            10, true, null, null, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("geschlossen", text);
        Assert.DoesNotContain("kurzzeitig", text);
    }

    [Fact]
    public void DoorAlert_Closed_WasNotOpenLongEnough_ContainsKurzzeitig()
    {
        var evt = new DoorMonitorAlertEvent(22, "Fenster", true, DoorMonitorAlertType.Closed,
            0, false, null, null, null, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("kurzzeitig", text);
    }

    [Fact]
    public void DoorAlert_Closed_HeatingRestored_ContainsGradGestellt()
    {
        var heatingChanges = new[] { new DoorMonitorHeatingChange("Schlafzimmer", 21.5f) };
        var evt = new DoorMonitorAlertEvent(22, "Fenster Schlafzimmer", true, DoorMonitorAlertType.Closed,
            30, true, null, null, heatingChanges, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("Schlafzimmer", text);
        Assert.Contains("Grad gestellt", text);
    }

    [Fact]
    public void DoorAlert_Closed_HeatingKeptOff_ContainsBleibtAus()
    {
        var heatingChanges = new[] { new DoorMonitorHeatingChange("Wohnzimmer", null) };
        var evt = new DoorMonitorAlertEvent(22, "Fenster Wohnzimmer", true, DoorMonitorAlertType.Closed,
            20, true, null, null, heatingChanges, null, false);

        string text = MonitoringRuleStore.BuildDoorAlertText(evt);

        Assert.Contains("bleibt aus", text);
    }
}
