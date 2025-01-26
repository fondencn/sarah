using Sarah.API.BusinessObjects;

namespace Sarah.Server.Models.Dtos;

/// <summary>
/// Represents a Data Transfer Object (DTO) for a device.
/// </summary>
public class DeviceDto
{
    /// <summary>
    /// Gets the ID of the device.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the Node ID of the device.
    /// </summary>
    /// <value>
    /// The Node ID is a byte value that uniquely identifies the device within a network.
    /// </value>
    public byte NodeId { get; set; }

    /// <summary>
    /// Gets the type name of the device.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets the name of the device.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets additional status information about the device.
    /// </summary>
    public string? Info { get; set; }

    /// <summary>
    /// gets the device type
    /// </summary>
    public KnownDeviceTypes DeviceType {get; set; }

    /// <summary>
    /// gets the room id
    /// </summary>
    public long? RoomId {get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is read-only.
    /// </summary>
    public bool IsReadonly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is a favourite.
    /// </summary>
    public bool IsFavourite {get; set; }
}
