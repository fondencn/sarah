using System.ComponentModel.DataAnnotations;

namespace Sarah.Rules.WebApi.Data.Entities;

public class RuleEntity
{
    public Guid Id { get; set; }
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public string? Action { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
