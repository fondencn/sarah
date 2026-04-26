using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;
using Sarah.ServiceClients;

namespace Sarah.Rules.Services.Kernel;

public sealed class SmartHomeKernelService
{
    private const string ConversationId = "smart-home-main";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SmartHomeKernelService> _logger;
    private readonly SmartHomePromptProvider _promptProvider;
    private readonly SemanticKernelOptions _options;

    public SmartHomeKernelService(
        IServiceScopeFactory scopeFactory,
        IOptions<SemanticKernelOptions> options,
        SmartHomePromptProvider promptProvider,
        ILogger<SmartHomeKernelService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _promptProvider = promptProvider;
        _options = options.Value;
    }

    public async Task ProcessEventAsync(NetworkEvent evt, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await PruneConversationHistoryAsync(db, cancellationToken);

        Microsoft.SemanticKernel.Kernel kernel = BuildKernel(scope.ServiceProvider);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        ChatHistory chatHistory = await BuildChatHistoryAsync(db, evt, cancellationToken);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        ChatMessageContent response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            kernel,
            cancellationToken);

        await PersistConversationTurnAsync(db, AuthorRole.User.Label, BuildEventPrompt(evt), cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            await PersistConversationTurnAsync(db, response.Role.Label, response.Content, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> PruneConversationHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        int removed = await PruneConversationHistoryAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return removed;
    }

    internal async Task<int> PruneConversationHistoryAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        DateTime cutoff = DateTime.UtcNow.AddHours(-_options.ConversationRetentionHours);
        var expired = await db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId && m.CreatedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            db.KernelConversationMessages.RemoveRange(expired);
        }

        return expired.Count;
    }

    internal string BuildEventPrompt(NetworkEvent evt)
    {
        string payload = JsonSerializer.Serialize(evt, evt.GetType(), new JsonSerializerOptions
        {
            WriteIndented = false
        });

        var sb = new StringBuilder();
        sb.AppendLine("Neues Smart-Home-Ereignis ist eingetroffen.");
        sb.AppendLine($"EventType: {evt.GetType().Name}");
        sb.AppendLine($"Property: {evt.Property}");
        sb.AppendLine($"SourceNodeId: {evt.SourceNodeId}");
        sb.AppendLine($"CreationDate: {evt.CreationDate:O}");
        sb.AppendLine("Payload:");
        sb.AppendLine(payload);
        sb.AppendLine("Pruefe die Legacy-Prompt-Regeln, den Verlauf und entscheide, ob du Plugins ausfuehren musst.");
        sb.AppendLine("Wenn eine Sprachausgabe erforderlich ist, verwende das Speech-Plugin.");
        return sb.ToString();
    }

    private Microsoft.SemanticKernel.Kernel BuildKernel(IServiceProvider serviceProvider)
    {
        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: _options.AzureOpenAI.DeploymentName,
            endpoint: _options.AzureOpenAI.Endpoint,
            apiKey: _options.AzureOpenAI.ApiKey,
            modelId: string.IsNullOrWhiteSpace(_options.AzureOpenAI.ModelId) ? null : _options.AzureOpenAI.ModelId);

        Microsoft.SemanticKernel.Kernel kernel = builder.Build();
        kernel.Plugins.AddFromObject(new SpeechKernelPlugin(
            serviceProvider.GetRequiredService<RabbitMQClient>(),
            serviceProvider.GetRequiredService<ILogger<SpeechKernelPlugin>>()), "speech");
        kernel.Plugins.AddFromObject(new AudioKernelPlugin(
            serviceProvider.GetRequiredService<RabbitMQClient>()), "audio");
        kernel.Plugins.AddFromObject(new DeviceControlKernelPlugin(
            serviceProvider.GetRequiredService<DeviceServiceClient>()), "devices");
        return kernel;
    }

    private async Task<ChatHistory> BuildChatHistoryAsync(ApplicationDbContext db, NetworkEvent evt, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(_promptProvider.BuildSystemPrompt());

        var messages = await db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(_options.MaxHistoryMessages)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            history.AddMessage(ParseRole(message.Role), message.Content);
        }

        history.AddUserMessage(BuildEventPrompt(evt));
        return history;
    }

    private async Task PersistConversationTurnAsync(ApplicationDbContext db, string role, string content, CancellationToken cancellationToken)
    {
        db.KernelConversationMessages.Add(new KernelConversationMessageEntity
        {
            ConversationId = ConversationId,
            Role = role,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        });

        await Task.CompletedTask;
    }

    private static AuthorRole ParseRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "assistant" => AuthorRole.Assistant,
            "tool" => AuthorRole.Tool,
            "system" => AuthorRole.System,
            _ => AuthorRole.User
        };
    }
}