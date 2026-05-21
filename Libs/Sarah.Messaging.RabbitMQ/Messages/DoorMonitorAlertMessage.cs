namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published by DoorMonitor for each door/window monitoring alert.
/// Carries all structured context needed for RuleService to generate voice output via a SayMessage.
/// </summary>
public class DoorMonitorAlertMessage : AbstractMessage
{
    /// <summary>
    /// ZWave node ID of the door/window sensor that triggered this alert.
    /// </summary>
    public byte SourceNodeId { get; set; }
    /// <summary>
    /// Human-readable device name of the door or window sensor.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// True if the sensor is a window ("Das Fenster"), false if it is a door ("Die Tür").
    /// </summary>
    public bool IsWindow { get; set; }

    /// <summary>
    /// The type of alert: device just opened, still open after a threshold, or just closed.
    /// </summary>
    public DoorAlertType AlertType { get; set; }

    /// <summary>
    /// How many full minutes the door/window has been open (used for StillOpen and Closed).
    /// </summary>
    public int OpenDurationMinutes { get; set; }

    /// <summary>
    /// True when the door was open for longer than the configured threshold.
    /// Used in Closed alerts to decide between "geschlossen" and "kurzzeitig offen gewesen".
    /// </summary>
    public bool WasOpenLongEnough { get; set; }

    /// <summary>
    /// Average room temperature at the time of the alert, if available.
    /// </summary>
    public float? RoomTemperature { get; set; }

    /// <summary>
    /// Detailed heating setpoint differentials caused by this door/window alert.
    /// Contains turn-off, restore, and skipped-restore transitions.
    /// </summary>
    public List<HeatingDifferentialInfo>? HeatingDifferentials { get; set; }

    /// <summary>
    /// Minutes until the next scheduled alert (e.g. 15, 30, 60, 120, 240).
    /// Null if no follow-up alert is planned.
    /// </summary>
    public int? NextAlertIntervalMinutes { get; set; }

    /// <summary>
    /// Speech volume to use for this alert.
    /// VeryLoud is used for security-critical sensors such as the front door.
    /// </summary>
    public SpeechVolume Volume { get; set; } = SpeechVolume.Normal;

    public DoorMonitorAlertMessage()
    {
        Topic = MessageTopics.MonitoringDoorAlert;
    }
}

/// <summary>
/// Heating state change associated with a door/window monitoring event.
/// </summary>
public class HeatingDifferentialInfo
{
    /// <summary>
    /// Name of the room whose heating was affected.
    /// </summary>
    public string RoomName { get; set; } = string.Empty;

    /// <summary>
    /// Previous target temperature before this change.
    /// </summary>
    public float PreviousTemperature { get; set; }

    /// <summary>
    /// Current/target temperature after this change.
    /// Null means no new target was set (for example, restore skipped because another window is still open).
    /// </summary>
    public float? CurrentTemperature { get; set; }

    /// <summary>
    /// Difference between current and previous target temperature.
    /// Null when no current target was applied.
    /// </summary>
    public float? TemperatureDelta { get; set; }

    /// <summary>
    /// Classifies why this differential was produced.
    /// </summary>
    public HeatingDifferentialType ChangeType { get; set; }
}

/// <summary>
/// Type of door/window monitoring alert.
/// </summary>
public enum DoorAlertType
{
    /// <summary>
    /// The door or window was just opened (immediate notification sensor).
    /// </summary>
    Opened,

    /// <summary>
    /// The door or window has been open for longer than the configured threshold.
    /// </summary>
    StillOpen,

    /// <summary>
    /// The door or window was closed after being monitored.
    /// </summary>
    Closed
}

/// <summary>
/// Type of heating differential produced by DoorMonitor.
/// </summary>
public enum HeatingDifferentialType
{
    TurnedOff = 0,
    Restored = 1,
    RestoreSkippedAnotherWindowOpen = 2
}
