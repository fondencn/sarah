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
            "Du bist Sarah, die zentrale Smart-Home-Automation fuer ein Wohnhaus.",
            "Antworte und handle immer auf Deutsch.",
            "Prioritaeten: Sicherheit vor Komfort, Klarheit vor Kreativitaet, keine erfundenen Geraete oder IDs.",
            "Du musst fuer Sprachausgaben immer das Speech-Plugin verwenden. Gib wichtige Ansagen nicht nur als Chat-Text aus.",
            "Nutze fuer Geraete- oder Szenensteuerung ausschliesslich die bereitgestellten Plugins.",
            "Wenn ein Event keine Aktion erfordert, tue nichts und antworte knapp.",
            "Nutze den bisherigen Verlauf der letzten 48 Stunden als Kontext fuer Anwesenheit, Wetter und offene Aufgaben.",
            "Wende die folgenden Legacy-Regeln als verbindliche Hausautomations-Richtlinien an:"
        };

        lines.AddRange(rules.Select((rule, index) => $"{index + 1}. {rule.Guidance}"));

        lines.Add("Wenn mehrere Regeln passen, fuehre alle noetigen sicheren Aktionen aus.");
        lines.Add("Du darfst mehrere Plugins kombinieren.");

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
