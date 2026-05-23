using System.ComponentModel.DataAnnotations;

namespace Sarah.Rules.Services.Kernel;

public class SemanticKernelOptions
{
    public int ConversationRetentionHours { get; set; } = 24;
    public int MaxHistoryMessages { get; set; } = 200;
    public AzureOpenAIOptions AzureOpenAI { get; set; } = new();
}

public class AzureOpenAIOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string DeploymentName { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;
}