using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.API.Interfaces.Services;

namespace Sarah.Rules.Services.Kernel;

public sealed class PresenceKernelPlugin
{
    private readonly IPersonService _personService;

    public PresenceKernelPlugin(IPersonService personService)
    {
        _personService = personService;
    }

    [KernelFunction, Description("Gibt eine Zusammenfassung der Anwesenheit von Personen zu Hause aus.")]
    public async Task<string> GetPresenceSummaryAsync()
    {
        var persons = (await _personService.GetAllPersonsAsync()).ToList();
        var atHome = persons.Where(p => p.IsAtHome).ToList();

        if (!persons.Any())
        {
            return "Keine Personen konfiguriert.";
        }

        if (!atHome.Any())
        {
            return $"Niemand ist zu Hause. Konfigurierte Personen: {string.Join(", ", persons.Select(p => p.Name))}.";
        }

        return $"{atHome.Count} von {persons.Count} Personen sind zu Hause: {string.Join(", ", atHome.Select(p => p.Name))}.";
    }

    [KernelFunction, Description("Prueft, ob aktuell jemand zu Hause ist.")]
    public async Task<string> IsSomeonePresentAsync()
    {
        bool present = await _personService.IsSomeonePresent();
        return present ? "Ja, mindestens eine Person ist zu Hause." : "Nein, aktuell ist niemand zu Hause.";
    }

    [KernelFunction, Description("Liefert die Namen aller Personen, die aktuell zu Hause sind.")]
    public async Task<string> GetAtHomePersonsAsync()
    {
        var atHome = (await _personService.GetAllPersonsAsync())
            .Where(p => p.IsAtHome)
            .Select(p => p.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        return atHome.Any()
            ? string.Join(", ", atHome)
            : "Keine Person ist aktuell zu Hause.";
    }
}
