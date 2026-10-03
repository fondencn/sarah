using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.Rules.Services.Clients;

namespace Sarah.Rules.Services.Kernel;


public sealed class GridStateForecastKernelPlugin
{
    private readonly StromGedachtGridStatesApiClient _gridStatesClient;
    private readonly IConfiguration _configuration;

    public GridStateForecastKernelPlugin(StromGedachtGridStatesApiClient gridStatesClient, IConfiguration configuration)
    {
        _gridStatesClient = gridStatesClient;
        _configuration = configuration;
    }

    [KernelFunction, Description("Liefert eine kurzfristige Empfehlung anhand der prognostizierten Stromnetzstufen (StromGedacht statesRelative).")]
    public async Task<string> GetGridStateAdvisoryAsync(int hoursInFuture = 12)
    {
        int windowHours = Math.Clamp(hoursInFuture, 1, 48);
        string zip = _configuration["StromGedacht:ZipCode"] ?? "71638";
        string? b2bId = _configuration["StromGedacht:B2BId"];

        var forecast = await _gridStatesClient.GetStatesRelativeAsync(
            zip: zip,
            hoursInFuture: windowHours,
            hoursInPast: 0,
            b2bId: b2bId);

        var states = forecast.States ?? new List<StromGedachtGridStateWindow>();
        if (states.Count == 0)
        {
            return $"Keine Stromnetzprognose fuer PLZ {zip} verfuegbar.";
        }

        var normalized = states.OrderBy(s => s.From).ToList();
        int worstState = normalized.Max(s => Priority(s.State));
        var bestGreenWindow = normalized
            .Where(s => s.State is -1 or 1)
            .OrderBy(s => s.From)
            .FirstOrDefault();

        string overall = worstState switch
        {
            >= 3 => "angespannt",
            2 => "mittel",
            _ => "entspannt"
        };

        string recommendation = worstState switch
        {
            >= 4 => "Empfehlung: vermeide flexible Grossverbraucher in diesem Zeitraum.",
            3 => "Empfehlung: verschiebe flexible Lasten wenn moeglich in gruenere Zeitfenster.",
            _ => "Empfehlung: Lastverschiebung aktuell nicht notwendig."
        };

        string greenHint = bestGreenWindow == null
            ? "Kein grueneres Zeitfenster innerhalb des Prognosehorizonts gefunden."
            : $"Naechstes gutes Zeitfenster: {bestGreenWindow.From:O} bis {bestGreenWindow.To:O} ({ToStateText(bestGreenWindow.State)}).";

        return $"Stromnetzprognose fuer PLZ {zip} (naechste {windowHours}h): insgesamt {overall}. " +
               $"{recommendation} {greenHint}";
    }

    private static int Priority(int state)
    {
        return state switch
        {
            4 => 4,
            3 => 3,
            1 => 2,
            -1 => 1,
            _ => 2
        };
    }

    private static string ToStateText(int state)
    {
        return state switch
        {
            -1 => "superGreen",
            1 => "green",
            3 => "orange",
            4 => "red",
            _ => "unknown"
        };
    }
}
