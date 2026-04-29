namespace Sarah.API.BusinessObjects.DTOs
{
    public class PromptRuleUpsertDto
    {
        public string Name { get; set; } = string.Empty;
        public string Guidance { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public int SortOrder { get; set; }
        public PromptRuleTimerDto? Timer { get; set; }
    }
}
