using System;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class RuleOverviewDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? LastOccurence { get; set; }
        public bool HasCondition { get; set; }
        public bool HasAction { get; set; }
    }
}
