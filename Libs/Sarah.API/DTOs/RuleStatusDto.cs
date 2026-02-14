using System;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class RuleStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int ActiveRules { get; set; }
        public DateTime? LastExecution { get; set; }
    }
}
