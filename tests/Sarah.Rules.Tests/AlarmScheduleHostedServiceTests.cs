using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sarah.Rules.Services;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Options;

namespace Sarah.Rules.Tests;

public class AlarmScheduleHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ThenStopAsync_DoesNotThrow()
    {
        using var provider = BuildServiceProvider();

        var scheduler = new AlarmScheduleService(
            logger: NullLogger<AlarmScheduleService>.Instance,
            rabbitMQ: null!,
            serviceScopeFactory: provider.GetRequiredService<IServiceScopeFactory>(),
            executionOptions: Options.Create(new AlarmExecutionOptions()));

        var hostedService = new AlarmScheduleHostedService(scheduler);

        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_IsIdempotent_WhenCalledMultipleTimes()
    {
        using var provider = BuildServiceProvider();

        var scheduler = new AlarmScheduleService(
            logger: NullLogger<AlarmScheduleService>.Instance,
            rabbitMQ: null!,
            serviceScopeFactory: provider.GetRequiredService<IServiceScopeFactory>(),
            executionOptions: Options.Create(new AlarmExecutionOptions()));

        var hostedService = new AlarmScheduleHostedService(scheduler);

        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services.BuildServiceProvider();
    }
}