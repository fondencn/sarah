using System.ComponentModel.DataAnnotations;

namespace Sarah.Monitoring.WebApi.Data.Entities;

public class MonitoringDataEntity
{
    public Guid Id { get; set; }
    [Required][MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    [Required][MaxLength(100)]
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
}
