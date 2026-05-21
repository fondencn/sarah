using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Sarah.API.BusinessObjects;
using Sarah.Rules.Services.Kernel;

namespace Sarah.Rules.Tests;

public class SmartHomeKernelServiceTests
{
    [Fact]
    public void BuildEventPrompt_SerializesEnumsAsStrings()
    {
        var service = new SmartHomeKernelService(
            scopeFactory: new NoopScopeFactory(),
            options: Options.Create(new SemanticKernelOptions()),
            promptProvider: new SmartHomePromptProvider(new NoopScopeFactory()),
            logger: NullLogger<SmartHomeKernelService>.Instance);

        var evt = new DoorMonitorAlertEvent(
            sourceNodeId: 22,
            deviceName: "Fenster Wohnzimmer",
            isWindow: true,
            alertType: DoorMonitorAlertType.StillOpen,
            openDurationMinutes: 10,
            wasOpenLongEnough: true,
            roomTemperature: 20.1f,
            heatingDifferentials: new[]
            {
                new DoorMonitorHeatingDifferential(
                    roomName: "Wohnzimmer",
                    previousTemperature: 21.5f,
                    currentTemperature: 12f,
                    temperatureDelta: -9.5f,
                    changeType: DoorMonitorHeatingDifferentialType.TurnedOff)
            },
            nextAlertIntervalMinutes: 15,
            isLoud: false);

        string prompt = service.BuildEventPrompt(evt);

        Assert.Contains("\"AlertType\":\"StillOpen\"", prompt);
        Assert.Contains("\"ChangeType\":\"TurnedOff\"", prompt);
    }

    private sealed class NoopScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new NoopScope();
    }

    private sealed class NoopScope : IServiceScope
    {
        public IServiceProvider ServiceProvider => new ServiceCollection().BuildServiceProvider();
        public void Dispose() { }
    }
}
