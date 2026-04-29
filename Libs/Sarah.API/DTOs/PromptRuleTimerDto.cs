using System;

namespace Sarah.API.BusinessObjects.DTOs
{
    public class PromptRuleTimerDto
    {
        public int Hour { get; set; }
        public int Minute { get; set; }
        public long Weekdays { get; set; }
        public int Interval { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? UntilUtc { get; set; }
    }
}
