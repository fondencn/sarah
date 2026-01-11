using System.ComponentModel.DataAnnotations;

namespace Sarah.EventProcessing.WebApi.Data.Entities;

public class EventEntity
{
    public Guid Id { get; set; }
    [Required][MaxLength(100)]
    public string Type { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
