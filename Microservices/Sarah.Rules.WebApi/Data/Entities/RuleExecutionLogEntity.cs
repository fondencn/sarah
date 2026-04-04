using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sarah.Rules.WebApi.Data.Entities;

[Table("RuleExecutionLogs")]
public class RuleExecutionLogEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string RuleName { get; set; } = string.Empty;

    public DateTime TriggeredAt { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }
}
