using System.ComponentModel;
using Microsoft.SemanticKernel;
using Sarah.API.Interfaces;

namespace Sarah.Rules.Services.Kernel;

public sealed class GridStateKernelPlugin
{
    private readonly IGridStateProvider _gridStateProvider;

    public GridStateKernelPlugin(IGridStateProvider gridStateProvider)
    {
        _gridStateProvider = gridStateProvider;
    }

    [KernelFunction, Description("Liefert den aktuellen Stromnetzstatus (StromGedacht Grid Stage) fuer die konfigurierte Region.")]
    public string GetCurrentGridState()
    {
        var state = _gridStateProvider.CurrentGridState;
        if (!state.HasValue)
        {
            return "Es liegen noch keine Stromnetzstatus-Daten vor.";
        }

        string zip = string.IsNullOrWhiteSpace(_gridStateProvider.CurrentZipCode)
            ? "unbekannt"
            : _gridStateProvider.CurrentZipCode;
        string changedAt = _gridStateProvider.LastChangedAtUtc?.ToString("O") ?? "unbekannt";

        return $"Aktueller Stromnetzstatus fuer PLZ {zip}: {_gridStateProvider.CurrentGridStateText} (Code {state.Value}). Letzte Aenderung: {changedAt}.";
    }
}
