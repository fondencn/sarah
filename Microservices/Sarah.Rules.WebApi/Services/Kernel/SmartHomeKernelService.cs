using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Rules.Services.Clients;
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
        _logger.LogInformation("Processing network event: {EventType} from node {NodeId} (property: {Property})",
            evt.GetType().Name, evt.SourceNodeId, evt.Property);

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

        _logger.LogDebug("Sending chat history with {MessageCount} messages to LLM", chatHistory.Count);

        ChatMessageContent response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            kernel,
            cancellationToken);

        _logger.LogDebug("LLM response received (role: {Role}, length: {Length})",
            response.Role.Label, response.Content?.Length ?? 0);

        await PersistConversationTurnAsync(db, AuthorRole.User.Label, BuildEventPrompt(evt), cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            await PersistConversationTurnAsync(db, response.Role.Label, response.Content, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Event processing complete for {EventType} from node {NodeId}",
            evt.GetType().Name, evt.SourceNodeId);
    }

    public async Task<string> ProcessChatMessageAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        return await ProcessChatMessageAsync(userMessage, targetSpeaker: string.Empty, cancellationToken);
    }

    public async Task<string> ProcessChatMessageAsync(string userMessage, string targetSpeaker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("userMessage must not be empty", nameof(userMessage));

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await PruneConversationHistoryAsync(db, cancellationToken);

        var speechContext = new ConversationSpeechContext(targetSpeaker);
        Microsoft.SemanticKernel.Kernel kernel = BuildKernel(scope.ServiceProvider, speechContext);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        ChatHistory chatHistory = await BuildChatHistoryAsync(db, userMessage, cancellationToken);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        ChatMessageContent response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            kernel,
            cancellationToken);

        await PersistConversationTurnAsync(db, AuthorRole.User.Label, userMessage.Trim(), cancellationToken);

        var assistantText = response.Content ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            await PersistConversationTurnAsync(db, response.Role.Label, assistantText, cancellationToken);

            if (!speechContext.HasSpeechOutput)
            {
                var rabbitMq = scope.ServiceProvider.GetRequiredService<RabbitMQClient>();
                await rabbitMq.PublishAsync(new SayMessage(assistantText, speechContext.TargetSpeaker));
                _logger.LogInformation("Published fallback speech response for speaker '{Speaker}'", speechContext.TargetSpeaker);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return assistantText;
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
        DateTime cutoff = GetConversationCutoffUtc(DateTime.UtcNow);
        var expired = await db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId && m.CreatedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            _logger.LogInformation("Pruning {Count} expired conversation messages older than {Cutoff:O}",
                expired.Count, cutoff);
            db.KernelConversationMessages.RemoveRange(expired);
        }
        else
        {
            _logger.LogDebug("No expired conversation messages to prune (cutoff: {Cutoff:O})", cutoff);
        }

        return expired.Count;
    }

    internal DateTime GetConversationCutoffUtc(DateTime utcNow)
    {
        return utcNow.AddHours(-_options.ConversationRetentionHours);
    }

    internal IQueryable<KernelConversationMessageEntity> QueryRetainedConversationMessages(ApplicationDbContext db, DateTime utcNow)
    {
        DateTime cutoff = GetConversationCutoffUtc(utcNow);

        return db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId && m.CreatedAtUtc >= cutoff)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(_options.MaxHistoryMessages);
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
        sb.AppendLine("Pruefe die Prompt-Regeln, den Verlauf und entscheide, ob du Plugins ausfuehren musst.");
        sb.AppendLine("Wenn eine Sprachausgabe erforderlich ist, verwende das Speech-Plugin.");
        return sb.ToString();
    }

    private Microsoft.SemanticKernel.Kernel BuildKernel(IServiceProvider serviceProvider, ConversationSpeechContext? speechContext = null)
    {
        _logger.LogDebug("Building kernel with deployment '{Deployment}' at {Endpoint}",
            _options.AzureOpenAI.DeploymentName, _options.AzureOpenAI.Endpoint);

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: _options.AzureOpenAI.DeploymentName,
            endpoint: _options.AzureOpenAI.Endpoint,
            apiKey: _options.AzureOpenAI.ApiKey,
            modelId: string.IsNullOrWhiteSpace(_options.AzureOpenAI.ModelId) ? null : _options.AzureOpenAI.ModelId);

        Microsoft.SemanticKernel.Kernel kernel = builder.Build();
        kernel.Plugins.AddFromObject(new SpeechKernelPlugin(
            serviceProvider.GetRequiredService<RabbitMQClient>(),
            speechContext,
            serviceProvider.GetRequiredService<ILogger<SpeechKernelPlugin>>()), "speech");
        kernel.Plugins.AddFromObject(new AudioKernelPlugin(
            serviceProvider.GetRequiredService<RabbitMQClient>()), "audio");
        kernel.Plugins.AddFromObject(new DeviceControlKernelPlugin(
            serviceProvider.GetRequiredService<DeviceServiceClient>()), "devices");
        kernel.Plugins.AddFromObject(new DeviceQueryKernelPlugin(
            serviceProvider.GetRequiredService<DeviceServiceClient>()), "deviceQuery");
        kernel.Plugins.AddFromObject(new RoomStateKernelPlugin(
            serviceProvider.GetRequiredService<RoomServiceClient>(),
            serviceProvider.GetRequiredService<DeviceServiceClient>()), "rooms");
        kernel.Plugins.AddFromObject(new PresenceKernelPlugin(
            serviceProvider.GetRequiredService<IPersonService>()), "presence");
        kernel.Plugins.AddFromObject(new WeatherKernelPlugin(
            serviceProvider.GetRequiredService<IWeatherProvider>()), "weather");
        kernel.Plugins.AddFromObject(new GridStateKernelPlugin(
            serviceProvider.GetRequiredService<IGridStateProvider>()), "gridState");
        kernel.Plugins.AddFromObject(new GridStateForecastKernelPlugin(
            serviceProvider.GetRequiredService<StromGedachtGridStatesApiClient>(),
            serviceProvider.GetRequiredService<IConfiguration>()), "gridStateForecast");
        kernel.Plugins.AddFromObject(new TimeContextKernelPlugin(
            serviceProvider.GetRequiredService<IConfiguration>()), "timeContext");
        kernel.Plugins.AddFromObject(new RulesMemoryKernelPlugin(
            serviceProvider.GetRequiredService<ApplicationDbContext>()), "rulesMemory");

        
        return kernel;
    }

    internal sealed class ConversationSpeechContext
    {
        public ConversationSpeechContext(string targetSpeaker)
        {
            TargetSpeaker = targetSpeaker ?? string.Empty;
        }

        public string TargetSpeaker { get; }

        public bool HasSpeechOutput { get; private set; }

        public void MarkSpeechOutput()
        {
            HasSpeechOutput = true;
        }
    }

    private async Task<ChatHistory> BuildChatHistoryAsync(ApplicationDbContext db, NetworkEvent evt, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(_promptProvider.BuildSystemPrompt());

        var messages = await QueryRetainedConversationMessages(db, DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            history.AddMessage(ParseRole(message.Role), message.Content);
        }

        history.AddUserMessage(BuildEventPrompt(evt));
        return history;
    }

    private async Task<ChatHistory> BuildChatHistoryAsync(ApplicationDbContext db, string userMessage, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(_promptProvider.BuildSystemPrompt());

        var messages = await QueryRetainedConversationMessages(db, DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            history.AddMessage(ParseRole(message.Role), message.Content);
        }

        history.AddUserMessage(userMessage.Trim());
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