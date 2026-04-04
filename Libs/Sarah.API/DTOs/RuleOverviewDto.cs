using System;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class RuleOverviewDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public string? Condition { get; set; }
        public string? Action { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
