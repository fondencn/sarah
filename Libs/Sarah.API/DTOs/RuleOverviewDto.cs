using System;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class RuleOverviewDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Condition { get; set; }
        public string? Action { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public DateTime? LastExecution { get; set; }
        public bool? LastSuccess { get; set; }
    }
}
