using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Monitoring.Clients;

namespace Sarah.Monitoring.Monitors;

internal sealed class GridStateMonitor(
    IConfiguration _config,
    RabbitMQClient _rabbitMq,
    StromGedachtNowApiClient _stromGedacht,
    ILogger<GridStateMonitor> _logger) : IMonitor, ICanSelfTest
{
    private Task? _updateTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private DateTime? _lastUpdateUtc;

    private int? _currentState;
    private readonly object _stateLock = new object();

    public Task Start()
    {
        if (_updateTask != null)
        {
            throw new InvalidOperationException("GridStateMonitor wurde bereits gestartet und kann nicht noch einmal gestartet werden.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _updateTask = Task.Run(() => UpdateLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        _logger.LogInformation("GridStateMonitor gestartet.");
        return Task.CompletedTask;
    }

    private async Task UpdateLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollCurrentStateAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren des Stromnetzstatus");
            }

            int pollSeconds = int.TryParse(_config["StromGedacht:PollIntervalSeconds"], out int cfgSeconds)
                ? Math.Clamp(cfgSeconds, 30, 3600)
                : 300;

            await Task.Delay(TimeSpan.FromSeconds(pollSeconds), cancellationToken);
        }
    }

    private async Task PollCurrentStateAsync(CancellationToken cancellationToken)
    {
        string zip = _config["StromGedacht:ZipCode"] ?? "71638";
        string? b2bId = _config["StromGedacht:B2BId"];

        StromGedachtNowResponse response = await _stromGedacht.GetNowAsync(
            zip: zip,
            hoursInFuture: null,
            b2bId: b2bId,
            cancellationToken: cancellationToken);

        int newState = response.State;
        int? previousState;
        bool changed;

        lock (_stateLock)
        {
            previousState = _currentState;
            changed = !_currentState.HasValue || _currentState.Value != newState;
            _currentState = newState;
            _lastUpdateUtc = DateTime.UtcNow;
        }

        if (!changed)
        {
            return;
        }

        string newText = ToGridStateText(newState);
        string? previousText = previousState.HasValue ? ToGridStateText(previousState.Value) : null;
        var message = new GridStateChangedMessage
        {
            ZipCode = zip,
            CurrentState = newState,
            CurrentStateText = newText,
            PreviousState = previousState,
            PreviousStateText = previousText,
            ChangedAtUtc = DateTime.UtcNow
        };

        await _rabbitMq.PublishAsync(message);
        _logger.LogInformation("StromGedacht: Grid state changed for zip {Zip}: {PreviousState} -> {CurrentState}", zip, previousText ?? "unknown", newText);
    }

    private static string ToGridStateText(int state)
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

    public IEnumerable<SelfTestResult> RunSelfTest()
    {
        if (!_lastUpdateUtc.HasValue)
        {
            yield return new SelfTestResult(true, "GridStateMonitor", "Noch keine Stromnetzstatus-Daten geladen.");
            yield break;
        }

        if ((DateTime.UtcNow - _lastUpdateUtc.Value) > TimeSpan.FromHours(1))
        {
            yield return new SelfTestResult(true, "GridStateMonitor", "Stromnetzstatus-Daten sind älter als 1 Stunde.");
        }
    }
}
