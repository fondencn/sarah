using Sarah.DeviceService.WebApi.Extensions;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class EnergyMeasurementThrottleTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FirstMeasurement_IsPublished() => Assert.True(new EnergyMeasurementThrottle().ShouldPublish(1, 100, T0));

    [Fact]
    public void WithinMinInterval_IsSuppressed()
    {
        var t = new EnergyMeasurementThrottle();
        t.ShouldPublish(1, 100, T0);
        Assert.False(t.ShouldPublish(1, 500, T0.AddSeconds(10)));
    }

    [Fact]
    public void SmallChange_IsSuppressed_UntilHeartbeat()
    {
        var t = new EnergyMeasurementThrottle();
        t.ShouldPublish(1, 100, T0);
        Assert.False(t.ShouldPublish(1, 101, T0.AddSeconds(60)));
        Assert.True(t.ShouldPublish(1, 101, T0.AddMinutes(6)));
    }

    [Fact]
    public void LargeChange_AfterMinInterval_IsPublished()
    {
        var t = new EnergyMeasurementThrottle();
        t.ShouldPublish(1, 100, T0);
        Assert.True(t.ShouldPublish(1, 150, T0.AddSeconds(31)));
    }

    [Fact]
    public void DevicesAreThrottledIndependently()
    {
        var t = new EnergyMeasurementThrottle();
        t.ShouldPublish(1, 100, T0);
        Assert.True(t.ShouldPublish(2, 100, T0.AddSeconds(1)));
    }
}
