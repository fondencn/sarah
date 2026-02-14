using Sarah.Messaging.RabbitMQ;

namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when holiday (Ferien) status changes (starts or ends).
/// Used to notify services to activate/deactivate holiday-dependent alarms.
/// </summary>
public class HolidayStatusChangedMessage : AbstractMessage
{
    /// <summary>
    /// Type of holiday status change
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// Holidays have started
        /// </summary>
        Started,

        /// <summary>
        /// Holidays have ended
        /// </summary>
        Ended
    }

    /// <summary>
    /// Name of the holidays (e.g., "Sommerferien", "Weihnachtsferien")
    /// </summary>
    public string HolidayName { get; set; } = string.Empty;

    /// <summary>
    /// Type of change that occurred
    /// </summary>
    public ChangeType Change { get; set; }

    /// <summary>
    /// Start date of the holiday period
    /// </summary>
    public DateTime HolidayStart { get; set; }

    /// <summary>
    /// End date of the holiday period
    /// </summary>
    public DateTime HolidayEnd { get; set; }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    public HolidayStatusChangedMessage()
    {
        Topic = MessageTopics.HolidaysStatusChanged;
    }

    /// <summary>
    /// Constructor for creating new holiday status change events
    /// </summary>
    /// <param name="holidayName">Name of the holidays</param>
    /// <param name="change">Type of change</param>
    /// <param name="holidayStart">Start date of holiday period</param>
    /// <param name="holidayEnd">End date of holiday period</param>
    public HolidayStatusChangedMessage(string HolidayName, ChangeType Change, DateTime HolidayStart, DateTime HolidayEnd)
    {
        Topic = MessageTopics.HolidaysStatusChanged;
        this.HolidayName = HolidayName;
        this.Change = Change;
        this.HolidayStart = HolidayStart;
        this.HolidayEnd = HolidayEnd;
    }
}
