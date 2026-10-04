using Sarah.DeviceService.WebApi.Extensions;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class EnergyMeasurementThrottleTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FirstMeasurement_IsPublished()
    {
        var throttle = new EnergyMeasurementThrottle();
        Assert.True(await throttle.PublishIfRequiredAsync(1, 100, T0, () => Task.CompletedTask));
    }

    [Fact]
    public async Task WithinMinInterval_IsSuppressed()
    {
        var t = new EnergyMeasurementThrottle();
        await t.PublishIfRequiredAsync(1, 100, T0, () => Task.CompletedTask);
        Assert.False(await t.PublishIfRequiredAsync(1, 500, T0.AddSeconds(10), () => Task.CompletedTask));
    }

    [Fact]
    public async Task SmallChange_IsSuppressed_UntilHeartbeat()
    {
        var t = new EnergyMeasurementThrottle();
        await t.PublishIfRequiredAsync(1, 100, T0, () => Task.CompletedTask);
        Assert.False(await t.PublishIfRequiredAsync(1, 101, T0.AddSeconds(60), () => Task.CompletedTask));
        Assert.True(await t.PublishIfRequiredAsync(1, 101, T0.AddMinutes(6), () => Task.CompletedTask));
    }

    [Fact]
    public async Task LargeChange_AfterMinInterval_IsPublished()
    {
        var t = new EnergyMeasurementThrottle();
        await t.PublishIfRequiredAsync(1, 100, T0, () => Task.CompletedTask);
        Assert.True(await t.PublishIfRequiredAsync(1, 150, T0.AddSeconds(31), () => Task.CompletedTask));
    }

    [Fact]
    public async Task FailedPublish_DoesNotAdvanceThrottleState()
    {
        var t = new EnergyMeasurementThrottle();
        await t.PublishIfRequiredAsync(1, 100, T0, () => Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            t.PublishIfRequiredAsync(1, 150, T0.AddSeconds(31), () => throw new InvalidOperationException()));

        Assert.True(await t.PublishIfRequiredAsync(1, 150, T0.AddSeconds(32), () => Task.CompletedTask));
    }

    [Fact]
    public async Task DevicesAreThrottledIndependently()
    {
        var t = new EnergyMeasurementThrottle();
        await t.PublishIfRequiredAsync(1, 100, T0, () => Task.CompletedTask);
        Assert.True(await t.PublishIfRequiredAsync(2, 100, T0.AddSeconds(1), () => Task.CompletedTask));
    }
}
