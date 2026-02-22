namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a person's home presence status changes
/// </summary>
public class PersonAvailabilityMessage : AbstractMessage
{
    /// <summary>
    /// Person's database ID
    /// </summary>
    public long PersonId { get; set; }

    /// <summary>
    /// Person's name
    /// </summary>
    public string PersonName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the person is available (at home)
    /// </summary>
    public bool IsAvailable { get; set; }

    public PersonAvailabilityMessage()
    {
        Topic = MessageTopics.PersonAvailability;
    }

    public PersonAvailabilityMessage(long PersonId, string PersonName, bool IsAvailable) : this()
    {
        this.PersonId = PersonId;
        this.PersonName = PersonName;
        this.IsAvailable = IsAvailable;
    }
}
