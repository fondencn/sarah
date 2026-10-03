using Microsoft.Extensions.Hosting;

namespace Sarah.Rules.Services;

/// <summary>
/// Hosts AlarmScheduleService in the ASP.NET Core hosted lifecycle.
/// </summary>
public sealed class AlarmScheduleHostedService : IHostedService
{
    private readonly AlarmScheduleService _alarmScheduleService;

    public AlarmScheduleHostedService(AlarmScheduleService alarmScheduleService)
    {
        _alarmScheduleService = alarmScheduleService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _alarmScheduleService.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _alarmScheduleService.Dispose();
        return Task.CompletedTask;
    }
}