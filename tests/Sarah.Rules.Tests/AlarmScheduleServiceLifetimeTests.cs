using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sarah.Rules.Data.Entities;
using Sarah.Rules.Services;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Options;

namespace Sarah.Rules.Tests;

public class AlarmScheduleServiceLifetimeTests
{
    [Fact]
    public async Task RefreshSchedulesAsync_AfterDispose_DoesNotThrow()
    {
        using var provider = BuildServiceProvider();
        var service = CreateService(provider);

        service.Dispose();

        var refreshMethod = typeof(AlarmScheduleService).GetMethod(
            "RefreshSchedulesAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(refreshMethod);

        var refreshTask = (Task?)refreshMethod!.Invoke(service, null);
        Assert.NotNull(refreshTask);

        await refreshTask!;
    }

    [Fact]
    public async Task GetActiveAlarmsAsync_ReturnsOnlyActiveRows_WhenServiceIsSingletonStyle()
    {
        using var provider = BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.AlarmSchedules.AddRange(
                new AlarmScheduleEntity
                {
                    AlarmTime = DateTime.UtcNow.AddMinutes(5),
                    Text = "active",
                    IsActive = true
                },
                new AlarmScheduleEntity
                {
                    AlarmTime = DateTime.UtcNow.AddMinutes(10),
                    Text = "inactive",
                    IsActive = false
                });

            await db.SaveChangesAsync();
        }

        var service = CreateService(provider);
        var active = (await service.GetActiveAlarmsAsync()).ToList();

        Assert.Single(active);
        Assert.Equal("active", active[0].Text);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddSingleton(databaseRoot);
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options.UseInMemoryDatabase("alarm-schedule-service-lifetime-tests", sp.GetRequiredService<InMemoryDatabaseRoot>()));
        return services.BuildServiceProvider();
    }

    private static AlarmScheduleService CreateService(IServiceProvider provider)
    {
        return new AlarmScheduleService(
            logger: NullLogger<AlarmScheduleService>.Instance,
            rabbitMQ: null!,
            serviceScopeFactory: provider.GetRequiredService<IServiceScopeFactory>(),
            executionOptions: Options.Create(new AlarmExecutionOptions()));
    }
}
