using System.ComponentModel.DataAnnotations;

namespace Sarah.Rules.WebApi.Data.Entities;

public class RuleExecutionLogEntity
{
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string RuleName { get; set; } = string.Empty;

    public DateTime ExecutedAt { get; set; }

    public bool Success { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? TriggerEventType { get; set; }
}
