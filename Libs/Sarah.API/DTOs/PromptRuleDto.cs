namespace Sarah.API.BusinessObjects.DTOs
{
    public class PromptRuleDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Guidance { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int SortOrder { get; set; }
        public PromptRuleTimerDto? Timer { get; set; }
    }
}
