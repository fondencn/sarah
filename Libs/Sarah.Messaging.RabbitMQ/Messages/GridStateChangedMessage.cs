namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when the current StromGedacht grid state changes.
/// </summary>
public class GridStateChangedMessage : AbstractMessage
{
    /// <summary>
    /// Zip code used for requesting the grid state.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// New grid state numeric value.
    /// Values currently are -1 (superGreen), 1 (green), 3 (orange), 4 (red).
    /// </summary>
    public int CurrentState { get; set; }

    /// <summary>
    /// Human readable text for CurrentState.
    /// </summary>
    public string CurrentStateText { get; set; } = string.Empty;

    /// <summary>
    /// Previous grid state numeric value, null on first observation.
    /// </summary>
    public int? PreviousState { get; set; }

    /// <summary>
    /// Human readable text for PreviousState.
    /// </summary>
    public string? PreviousStateText { get; set; }

    /// <summary>
    /// UTC timestamp when the state change was detected.
    /// </summary>
    public DateTime ChangedAtUtc { get; set; }

    public GridStateChangedMessage()
    {
        Topic = MessageTopics.MonitoringGridStateChanged;
    }
}
