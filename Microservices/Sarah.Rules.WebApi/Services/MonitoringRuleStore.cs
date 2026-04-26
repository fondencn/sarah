using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Rules.Actions;
using Sarah.Rules.Conditions;
using Microsoft.Extensions.Logging;

namespace Sarah.Rules;

/// <summary>
/// Rule store that generates speech output for monitor events (battery warnings and door/window alerts).
/// This centralises all speech text generation that was previously done inside the individual monitors.
/// </summary>
public class MonitoringRuleStore : IRuleStore
{
    private readonly RabbitMQClient _rabbitMQ;
    private readonly ILogger<MonitoringRuleStore> _logger;
    private readonly List<Rule> _rules;

    public IReadOnlyCollection<Rule> Rules => _rules.AsReadOnly();

    // Never fires – rules are fixed at construction time.
    public event EventHandler? Changed;

    public MonitoringRuleStore(RabbitMQClient rabbitMQ, ILogger<MonitoringRuleStore> logger)
    {
        _rabbitMQ = rabbitMQ;
        _logger = logger;
        _rules = new List<Rule>();
        CreateRules();
    }

    private void CreateRules()
    {
        AddBatteryWarningRules();
        AddDoorMonitorAlertRules();
    }

    // ─── Battery warning ────────────────────────────────────────────────────────

    private void AddBatteryWarningRules()
    {
        _rules.Add(new Rule
        {
            Condition = new PredicateCondition(0, evt => evt is BatteryWarningEvent bwe && bwe.Warnings.Count > 0),
            Action = new ActionRuleAction(evt =>
            {
                var bwe = (BatteryWarningEvent)evt;
                string text = BuildBatteryWarningText(bwe);
                _rabbitMQ.PublishAsync(new SayMessage(text, "")).GetAwaiter().GetResult();
            }, _logger),
            Name = "Sprachausgabe für Batterie-Warnungen des BatteryMonitors"
        });
    }

    internal static string BuildBatteryWarningText(BatteryWarningEvent bwe)
    {
        var sb = new System.Text.StringBuilder("Achtung, Ladezustand kritisch:");
        foreach (var warning in bwe.Warnings)
        {
            sb.Append(' ');
            if (warning.BatteryLevel < 5)
                sb.Append($"{warning.DeviceName}: Batterie leer.");
            else
                sb.Append($"{warning.DeviceName}: Weniger als {warning.BatteryLevel:F0} Prozent Batterieladung.");
        }
        return sb.ToString();
    }

    // ─── Door / window monitor alerts ──────────────────────────────────────────

    private void AddDoorMonitorAlertRules()
    {
        _rules.Add(new Rule
        {
            Condition = new PredicateCondition(0, evt => evt is DoorMonitorAlertEvent),
            Action = new ActionRuleAction(evt =>
            {
                var alert = (DoorMonitorAlertEvent)evt;
                string text = BuildDoorAlertText(alert);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var volume = alert.IsLoud
                        ? Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.VeryLoud
                        : Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.Normal;
                    _rabbitMQ.PublishAsync(new SayMessage(text, "", volume)).GetAwaiter().GetResult();
                }
            }, _logger),
            Name = "Sprachausgabe für Tür-/Fenster-Meldungen des DoorMonitors"
        });
    }

    internal static string BuildDoorAlertText(DoorMonitorAlertEvent alert)
    {
        string artikel = alert.IsWindow ? "Das" : "Die";

        switch (alert.AlertType)
        {
            case DoorMonitorAlertType.Opened:
                return $"{artikel} {alert.DeviceName} wurde geöffnet.";

            case DoorMonitorAlertType.StillOpen:
            {
                string baseText = alert.OpenDurationMinutes switch
                {
                    0 => $"{artikel} {alert.DeviceName} ist seit kurzem offen.",
                    1 => $"{artikel} {alert.DeviceName} ist eine Minute offen.",
                    _ => $"{artikel} {alert.DeviceName} ist {alert.OpenDurationMinutes} Minuten offen."
                };

                var sb = new System.Text.StringBuilder(baseText);

                // Next alert interval
                if (alert.NextAlertIntervalMinutes.HasValue)
                {
                    string intervalText = alert.NextAlertIntervalMinutes.Value switch
                    {
                        60  => "einer Stunde",
                        120 => "zwei Stunden",
                        240 => "vier Stunden",
                        var m => $"{m} Minuten"
                    };
                    sb.Append($" Die nächste Information erfolgt in {intervalText}.");
                }

                // Room temperature
                if (alert.RoomTemperature.HasValue)
                {
                    if (alert.RoomTemperature.Value < 18f)
                        sb.Append($" Die Raumtemperatur beträgt nur noch {alert.RoomTemperature.Value:F1} Grad.");
                    else
                        sb.Append($" Die Raumtemperatur beträgt {alert.RoomTemperature.Value:F1} Grad.");
                }

                // Heatings turned off
                if (alert.HeatingsTurnedOff?.Count > 0)
                {
                    string rooms = string.Join(" und ", alert.HeatingsTurnedOff);
                    bool plural = alert.HeatingsTurnedOff.Count > 1;
                    sb.Append($" Heizung{(plural ? "en" : "")} im {rooms} ausgeschalt{(plural ? "en" : "et")}.");
                }

                return sb.ToString();
            }

            case DoorMonitorAlertType.Closed:
            {
                string baseText = alert.WasOpenLongEnough
                    ? $"{artikel} {alert.DeviceName} ist geschlossen."
                    : $"{artikel} {alert.DeviceName} ist kurzzeitig offen gewesen.";

                if (alert.HeatingChanges?.Count > 0)
                {
                    var sb = new System.Text.StringBuilder(baseText);
                    var parts = new List<string>();
                    foreach (var change in alert.HeatingChanges)
                    {
                        if (change.RestoredTemperature.HasValue)
                            parts.Add($" im {change.RoomName} auf {change.RestoredTemperature.Value:F1} Grad gestellt");
                        else
                            parts.Add($" im {change.RoomName} bleibt aus wegen anderes offenes Fenster");
                    }
                    bool plural = alert.HeatingChanges.Count > 1;
                    sb.Append($" Heizung{(plural ? "en" : "")}{string.Join(" und", parts)}.");
                    return sb.ToString();
                }

                return baseText;
            }

            default:
                return string.Empty;
        }
    }
}
