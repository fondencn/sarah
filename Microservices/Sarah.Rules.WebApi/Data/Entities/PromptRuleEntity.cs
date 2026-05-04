using System.ComponentModel.DataAnnotations;
using Sarah.API.BusinessObjects;

namespace Sarah.Rules.WebApi.Data.Entities;

public class PromptRuleEntity
{
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Guidance { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; }

    public int? TimerHour { get; set; }

    public int? TimerMinute { get; set; }

    public Weekdays? TimerWeekdays { get; set; }

    public RecurrenceInterval? TimerInterval { get; set; }

    public DateTime? TimerFromUtc { get; set; }

    public DateTime? TimerUntilUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}