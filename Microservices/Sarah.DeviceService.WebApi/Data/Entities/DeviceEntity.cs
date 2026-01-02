using System.ComponentModel.DataAnnotations;

namespace Sarah.DeviceService.WebApi.Data.Entities;

public class DeviceEntity
{
    public Guid Id { get; set; }
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required][MaxLength(100)]
    public string Type { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Status { get; set; } = "Unknown";
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Metadata { get; set; }
    public bool IsOnline { get; set; }
}
