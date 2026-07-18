using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Rules.Conditions;
using Microsoft.EntityFrameworkCore;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;

namespace Sarah.Rules.Services.Kernel;

public sealed class SmartHomePromptProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _sync = new();
    private List<PromptRuleDefinition> _rules;

    public SmartHomePromptProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _rules = new List<PromptRuleDefinition>();
    }

    public IReadOnlyList<PromptRuleDefinition> PromptRules
    {
        get
        {
            lock (_sync)
            {
                return _rules.ToList();
            }
        }
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var dbRules = await db.PromptRules
            .AsNoTracking()
            .Where(rule => rule.IsEnabled)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Name)
            .ToListAsync(cancellationToken);

        var loadedRules = dbRules.Select(MapFromEntity).ToList();

        lock (_sync)
        {
            _rules = loadedRules;
        }
    }

    public string BuildSystemPrompt()
    {
        var rules = PromptRules;
        var lines = new List<string>
        {
            "Identität: Sarah, freundliche Hausautomations-KI für diese Wohnung.",
            "Aufgabe: Regeln befolgen, Entscheidungen ausführen, keine unnötige Erklärung.",
            "Ziel: Sicherheit vor Komfort, Klarheit vor Kreativität, keine erfundenen Geräte oder IDs.",
            "Nutze für Geräte-, Szenen- und Sprachausgaben nur die bereitgestellten Plugins.",
            "Für Sprachausgaben immer das Speech-Plugin verwenden.",
            "Wenn du zusätzliche Informationen brauchst, hole sie zuerst mit den verfügbaren Plugins.",
            "Wenn eine Regel weitere Verarbeitung verbietet, brich sofort ab und antworte nur 'ok'.",
            "Für Regeln mit Zeitbezug: frage vorher die Uhrzeit über das Plugin im 24h-Format ab.",
            "Wenn mehrere Regeln passen, führe alle notwendigen Aktionen aus.",
            "Beschränkungen und gezielte Anweisungen die sich auf die Eigenschaft SourceNodeid beziehen, sind exakt zu beachten. ",
            "Wenn keine Aktion erforderlich ist, antworte nur '.' (Keine Sprachausgabe!). ",
            "Wenn du eine Sprachausgabe ausgibst oder eine Aktions auslöst, schreib das in deine Antwort. ", 
            "Wende die folgenden Regeln als verbindlich an:"
        };

        lines.AddRange(rules.Select((rule, index) => $"{index + 1}. {rule.Guidance}"));

        return string.Join(Environment.NewLine, lines);
    }

    private static PromptRuleDefinition MapFromEntity(PromptRuleEntity entity)
    {
        TimerCondition? timerCondition = null;
        if (entity.TimerHour.HasValue && entity.TimerMinute.HasValue && entity.TimerWeekdays.HasValue)
        {
            var recurrence = new TimerRecurrence
            {
                Hour = entity.TimerHour.Value,
                Minute = entity.TimerMinute.Value,
                Weekdays = entity.TimerWeekdays.Value,
                Interval = entity.TimerInterval ?? RecurrenceInterval.Täglich,
                From = entity.TimerFromUtc,
                Until = entity.TimerUntilUtc
            };

            timerCondition = new TimerCondition(recurrence);
        }

        return new PromptRuleDefinition(entity.Name, entity.Guidance, timerCondition);
    }
}

public sealed class SmartHomePromptRuleStore : IRuleStore
{
    private readonly SmartHomePromptProvider _promptProvider;
    private readonly object _sync = new();
    private List<Rule> _rules;

    public SmartHomePromptRuleStore(SmartHomePromptProvider promptProvider)
    {
        _promptProvider = promptProvider;
        _rules = new List<Rule>();
        ReloadFromProvider();
    }

    public IReadOnlyCollection<Rule> Rules
    {
        get
        {
            lock (_sync)
            {
                return _rules.ToList().AsReadOnly();
            }
        }
    }

    public void ReloadFromProvider()
    {
        lock (_sync)
        {
            _rules = _promptProvider.PromptRules.Select(rule => rule.ToRule()).ToList();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;
}

public sealed class PromptRuleDefinition
{
    public PromptRuleDefinition(string name, string guidance, TimerCondition? timerCondition)
    {
        Name = name;
        Guidance = guidance;
        TimerCondition = timerCondition;
    }

    public string Name { get; }
    public string Guidance { get; }
    public TimerCondition? TimerCondition { get; }

    public Rule ToRule()
    {
        return new Rule
        {
            Name = Name,
            Condition = TimerCondition
        };
    }
}
