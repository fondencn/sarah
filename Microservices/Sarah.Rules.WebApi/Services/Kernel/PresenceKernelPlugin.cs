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

    public async Task<string> GetLocationOfPerson(string personName)
    {
        var person = (await _personService.GetAllPersonsAsync())
            .FirstOrDefault(p => string.Equals(p.Name, personName, StringComparison.OrdinalIgnoreCase));

        if (person == null)
        {
            return $"Keine Person mit Namen '{personName}' gefunden.";
        }

        if (person.IsAtHome)
        {
            return $"{person.Name} ist zu Hause.";
        }

        // Get GeoFence of Person
        if (person.CurrentGeoFence != null)
        {
            return $"{person.Name} befindet sich an folgendem Ort: '{person.CurrentGeoFence.Name}'.";
        }
        // Use current location of person
        if (!string.IsNullOrWhiteSpace(person.CurrentNamedPosition))
        {
            return $"{person.Name} befindet sich an folgendem Ort: '{person.CurrentNamedPosition}'.";
        }
        if (person.CurrentPositionLat.HasValue && person.CurrentPositionLong.HasValue)  
        {
            return $"{person.Name} befindet sich aktuell an Position {person.CurrentPositionLat.Value}°, {person.CurrentPositionLong.Value}°.";
        }

        return $"{person.Name} kann nicht lokalisiert werden.";
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
