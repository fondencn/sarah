using System;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class RuleExecutionLogDto
    {
        public long Id { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TriggerEventType { get; set; }
    }
}
