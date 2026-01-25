namespace Sarah.Messaging.RabbitMQ.Messages;

/// <summary>
/// Message published when a person enters or leaves a geofence
/// </summary>
public class PersonGeoFenceMessage : AbstractMessage
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
    /// Name of the current geofence (null if none)
    /// </summary>
    public string? CurrentGeoFenceName { get; set; }

    /// <summary>
    /// Name of the previous geofence (null if none)
    /// </summary>
    public string? PreviousGeoFenceName { get; set; }

    public PersonGeoFenceMessage()
    {
        Topic = MessageTopics.PersonGeoFence;
    }

    public PersonGeoFenceMessage(long personId, string personName, string? currentGeoFenceName, string? previousGeoFenceName) : this()
    {
        PersonId = personId;
        PersonName = personName;
        CurrentGeoFenceName = currentGeoFenceName;
        PreviousGeoFenceName = previousGeoFenceName;
    }
}
