using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Monitoring.WebApi.Data.Entities;

/// <summary>
/// A database entry for an event triggered by a device
/// </summary>
[Table("DeviceTraces")]
public class DeviceTraceEntity
{
    /// <summary>
    /// ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Zwave Node ID of the device
    /// </summary>
    public byte NodeId { get; set; }

    /// <summary>
    /// Time of occurrence
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Log text
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Name of the changed property
    /// </summary>
    public string? Property { get; set; }

    /// <summary>
    /// New value of the changed property
    /// </summary>
    public string? Value { get; set; }
}
