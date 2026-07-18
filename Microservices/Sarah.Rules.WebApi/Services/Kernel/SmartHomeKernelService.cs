using System.Text;
using System.Text.Json;
using System.Diagnostics;
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

    public async Task ProcessEventAsync(NetworkEvent evt, string? deviceName = null, CancellationToken cancellationToken = default)
    {
        var traceId = Guid.NewGuid().ToString("N")[..8];
        var totalTimer = Stopwatch.StartNew();

        _logger.LogInformation("Processing network event: {EventType} from node {NodeId} (property: {Property}, deviceName: {DeviceName})",
            evt.GetType().Name, evt.SourceNodeId, evt.Property, deviceName ?? "unknown");

        _logger.LogInformation("Kernel event trace {TraceId} started", traceId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pruneTimer = Stopwatch.StartNew();
        await PruneConversationHistoryAsync(db, cancellationToken);
        pruneTimer.Stop();
        _logger.LogInformation("Kernel event trace {TraceId} stage prune_history_ms={DurationMs}", traceId, pruneTimer.ElapsedMilliseconds);

        var kernelBuildTimer = Stopwatch.StartNew();
        Microsoft.SemanticKernel.Kernel kernel = BuildKernel(scope.ServiceProvider);
        kernelBuildTimer.Stop();
        _logger.LogInformation("Kernel event trace {TraceId} stage build_kernel_ms={DurationMs}", traceId, kernelBuildTimer.ElapsedMilliseconds);

        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var historyTimer = Stopwatch.StartNew();
        ChatHistory chatHistory = await BuildChatHistoryAsync(db, evt, deviceName, cancellationToken);
        historyTimer.Stop();
        _logger.LogInformation("Kernel event trace {TraceId} stage build_history_ms={DurationMs} message_count={MessageCount}",
            traceId, historyTimer.ElapsedMilliseconds, chatHistory.Count);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        _logger.LogDebug("Sending chat history with {MessageCount} messages to LLM", chatHistory.Count);

        var llmTimer = Stopwatch.StartNew();
        ChatMessageContent response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            kernel,
            cancellationToken);
        llmTimer.Stop();
        _logger.LogInformation("Kernel event trace {TraceId} stage llm_call_ms={DurationMs}", traceId, llmTimer.ElapsedMilliseconds);

        _logger.LogDebug("LLM response received (role: {Role}, length: {Length})",
            response.Role.Label, response.Content?.Length ?? 0);

        var persistTimer = Stopwatch.StartNew();
        await PersistConversationTurnAsync(db, AuthorRole.User.Label, BuildEventPrompt(evt, deviceName), cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            await PersistConversationTurnAsync(db, response.Role.Label, response.Content, cancellationToken);
        }

        persistTimer.Stop();
        _logger.LogInformation("Kernel event trace {TraceId} stage persist_turns_ms={DurationMs}", traceId, persistTimer.ElapsedMilliseconds);

        var saveTimer = Stopwatch.StartNew();
        await db.SaveChangesAsync(cancellationToken);
        saveTimer.Stop();
        _logger.LogInformation("Kernel event trace {TraceId} stage save_changes_ms={DurationMs}", traceId, saveTimer.ElapsedMilliseconds);

        totalTimer.Stop();
        _logger.LogInformation("Kernel event trace {TraceId} completed total_ms={DurationMs}", traceId, totalTimer.ElapsedMilliseconds);

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

        var traceId = Guid.NewGuid().ToString("N")[..8];
        var totalTimer = Stopwatch.StartNew();
        _logger.LogInformation("Kernel chat trace {TraceId} started target_speaker='{Speaker}' message_length={Length}",
            traceId, targetSpeaker, userMessage.Length);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pruneTimer = Stopwatch.StartNew();
        await PruneConversationHistoryAsync(db, cancellationToken);
        pruneTimer.Stop();
        _logger.LogInformation("Kernel chat trace {TraceId} stage prune_history_ms={DurationMs}", traceId, pruneTimer.ElapsedMilliseconds);

        var speechContext = new ConversationSpeechContext(targetSpeaker);

        var kernelBuildTimer = Stopwatch.StartNew();
        Microsoft.SemanticKernel.Kernel kernel = BuildKernel(scope.ServiceProvider, speechContext);
        kernelBuildTimer.Stop();
        _logger.LogInformation("Kernel chat trace {TraceId} stage build_kernel_ms={DurationMs}", traceId, kernelBuildTimer.ElapsedMilliseconds);

        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var historyTimer = Stopwatch.StartNew();
        ChatHistory chatHistory = await BuildChatHistoryAsync(db, userMessage, cancellationToken);
        historyTimer.Stop();
        _logger.LogInformation("Kernel chat trace {TraceId} stage build_history_ms={DurationMs} message_count={MessageCount}",
            traceId, historyTimer.ElapsedMilliseconds, chatHistory.Count);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var llmTimer = Stopwatch.StartNew();
        ChatMessageContent response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            kernel,
            cancellationToken);
        llmTimer.Stop();
        _logger.LogInformation("Kernel chat trace {TraceId} stage llm_call_ms={DurationMs}", traceId, llmTimer.ElapsedMilliseconds);

        var persistTimer = Stopwatch.StartNew();
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

        persistTimer.Stop();
        _logger.LogInformation("Kernel chat trace {TraceId} stage persist_turns_ms={DurationMs}", traceId, persistTimer.ElapsedMilliseconds);

        var saveTimer = Stopwatch.StartNew();
        await db.SaveChangesAsync(cancellationToken);
        saveTimer.Stop();
        _logger.LogInformation("Kernel chat trace {TraceId} stage save_changes_ms={DurationMs}", traceId, saveTimer.ElapsedMilliseconds);

        totalTimer.Stop();
        _logger.LogInformation("Kernel chat trace {TraceId} completed total_ms={DurationMs} assistant_length={Length}",
            traceId, totalTimer.ElapsedMilliseconds, assistantText.Length);

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

    internal string BuildEventPrompt(NetworkEvent evt, string? deviceName = null)
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
        if (!string.IsNullOrWhiteSpace(deviceName))
            sb.AppendLine($"DeviceName: {deviceName}");
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

    private async Task<ChatHistory> BuildChatHistoryAsync(ApplicationDbContext db, NetworkEvent evt, string? deviceName, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(_promptProvider.BuildSystemPrompt());

        var messages = await QueryRetainedConversationMessages(db, DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            history.AddMessage(ParseRole(message.Role), message.Content);
        }

        history.AddUserMessage(BuildEventPrompt(evt, deviceName));
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