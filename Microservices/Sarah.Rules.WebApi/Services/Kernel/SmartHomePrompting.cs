using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Rules.Conditions;

namespace Sarah.Rules.Services.Kernel;

public sealed class SmartHomePromptProvider
{
    private readonly IReadOnlyList<PromptRuleDefinition> _rules;

    public SmartHomePromptProvider()
    {
        _rules = CreateRules();
    }

    public IReadOnlyList<PromptRuleDefinition> PromptRules => _rules;

    public string BuildSystemPrompt()
    {
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

        lines.AddRange(_rules.Select((rule, index) => $"{index + 1}. {rule.Guidance}"));

        lines.Add("Wenn mehrere Regeln passen, fuehre alle noetigen sicheren Aktionen aus.");
        lines.Add("Bei Alarm- oder Sicherheitsereignissen darfst du mehrere Plugins kombinieren.");

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<PromptRuleDefinition> CreateRules()
    {
        return new List<PromptRuleDefinition>
        {
            new("Keyfob41 Schalter 1 schaltet LED 30 an/aus", "Wenn ein ClickedEvent von Node 41 mit SceneId 1 kommt, schalte Lampe Node 30 um.", null),
            new("Keyfob41 Schalter 2 schaltet Lampe 21 an/aus", "Wenn ein ClickedEvent von Node 41 mit SceneId 2 kommt, schalte Lampe Node 21 um.", null),
            new("Tuere Arbeitszimmer offen ohne bekannte Person", "Wenn die Tuer im Arbeitszimmer offen ist und keine bekannte Person zuhause ist, melde das per Sprache.", null),
            new("Haustuere offen ohne bekannte Person", "Wenn die Haustuer offen ist und keine bekannte Person zuhause ist, gib eine sehr laute Warnung aus, starte alert1.wav und aktiviere die Szene RedAlert.", null),
            new("Haustuere geschlossen beendet Alarm", "Wenn die Haustuer geschlossen wird und jemand zuhause ist, stoppe die Szene RedAlert und stoppe die Audio-Wiedergabe.", null),
            new("Lampe 14 bei Praesenz im Arbeitszimmer", "Wenn im Arbeitszimmer Praesenz erkannt wird und die Helligkeit unter 40 liegt, setze Lampe 14 auf Helligkeit 255 und Farbe #FFBC1F.", null),
            new("Morgens Guten Morgen sagen", "Wenn Christian morgens zwischen 7 und 10 Uhr im Arbeitszimmer anwesend ist, gib einmal pro Tag eine Begruessung mit Uhrzeit, aktuellem Wetter und Wettervorhersage auf speaker1 aus.", null),
            new("Lampe 14 aus bei keiner Praesenz", "Wenn im Arbeitszimmer keine Praesenz mehr erkannt wird, schalte Lampe 14 aus.", null),
            new("Trockner fertig", "Wenn WallPlug Node 251 auf aus/faellt, sage: Der Waeschetrockner ist fertig.", null),
            new("Waschmaschine fertig", "Wenn WallPlug Node 252 auf aus/faellt, sage: Die Waschmaschine ist fertig.", null),
            new("Kaffeemaschine fertig", "Wenn WallPlug Node 20 auf aus/faellt, schalte Node 20 aus und sage: Der Kaffee ist fertig.", null),
            new("Kaffeemaschine an bei Christian Heimkehr", "Wenn Christian zwischen 7 und 11 Uhr nach Hause kommt, schalte Steckdose Node 20 ein und sage, dass die Kaffeemaschine eingeschaltet wird.", null),
            new("Luftqualitaet Arbeitszimmer", "Wenn die Luftqualitaet im Arbeitszimmer kritisch ist und dort jemand anwesend ist, gib eine passende Luftqualitaetsansage aus.", null),
            new("Feueralarm Node 38", "Wenn Rauchmelder Node 38 Feueralarm meldet, sage laut, dass Rauchmelder 38 Feueralarm meldet.", null),
            new("Tracker SOS Lukas", "Wenn Tracker Node 247 den SOS-Knopf drueckt, sage: Warnung: Lukas hat den SOS Knopf seines Trackers gedrueckt.", null),
            new("Tracker SOS Christian", "Wenn Tracker Node 246 den SOS-Knopf drueckt, sage: Warnung: Christian hat den SOS Knopf seines Trackers gedrueckt.", null),
            new("Tracker SOS Hannah", "Wenn Tracker Node 245 den SOS-Knopf drueckt, sage: Warnung: Hannah hat den SOSKnopf ihres Trackers gedrueckt.", null),
            new("Mittagspause Erinnerung", "Um 12:00 Uhr an Werktagen erinnere an die Mittagspause auf speaker1 und setze Lampe 14 auf rot.",
                new TimerCondition(new TimerRecurrence { Hour = 12, Minute = 0, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag })),
            new("Ende Mittagspause Erinnerung", "Um 14:00 Uhr an Werktagen, wenn jemand im Arbeitszimmer ist, erinnere an das Weiterarbeiten auf speaker1 und schalte Lampe 14 aus.",
                new TimerCondition(new TimerRecurrence { Hour = 14, Minute = 0, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag })),
            new("Daily Erinnerung 8:00", "Um 8:00 Uhr an Werktagen, wenn jemand im Arbeitszimmer ist, sage: Es ist Zeit fuers Daily, und setze Lampe 14 auf blau.",
                new TimerCondition(new TimerRecurrence { Hour = 8, Minute = 0, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag })),
            new("Daily Ende 8:20", "Um 8:20 Uhr an Werktagen, wenn jemand im Arbeitszimmer ist, sage: Das Daily sollte nun zu Ende sein, und setze Lampe 14 auf gruen.",
                new TimerCondition(new TimerRecurrence { Hour = 8, Minute = 20, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag })),
            new("Lampe Wohnzimmer aus 6:45", "Um 6:45 Uhr an Werktagen schalte Lampe 24 aus.",
                new TimerCondition(new TimerRecurrence { Hour = 6, Minute = 45, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag })),
            new("Daily Lampe aus 8:21", "Um 8:21 Uhr an Werktagen schalte Lampe 14 aus.",
                new TimerCondition(new TimerRecurrence { Hour = 8, Minute = 21, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag })),
            new("Batteriewarnungen", "Wenn BatteryWarningEvent kritische Batterien meldet, gib eine Sprachausgabe mit den betroffenen Geraeten aus.", null),
            new("DoorMonitor Meldungen", "Wenn DoorMonitorAlertEvent ein Fenster oder eine Tuer meldet, gib die passende Sprachausgabe aus. Bei IsLoud=true verwende sehr laute Lautstaerke.", null),
            new("Wetterwarnungen", "Wenn WeatherWarningEvent eine Warnung enthaelt, gib eine Wetterwarnung per Sprache aus.", null),
            new("Wettervorhersage abends", "Wenn zwischen 18 und 19 Uhr eine Wettervorhersage fuer heute aktualisiert wird, gib die Wettervorhersage per Sprache aus.", null)
        };
    }
}

public sealed class SmartHomePromptRuleStore : IRuleStore
{
    private readonly List<Rule> _rules;

    public SmartHomePromptRuleStore(SmartHomePromptProvider promptProvider)
    {
        _rules = promptProvider.PromptRules.Select(rule => rule.ToRule()).ToList();
    }

    public IReadOnlyCollection<Rule> Rules => _rules.AsReadOnly();

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