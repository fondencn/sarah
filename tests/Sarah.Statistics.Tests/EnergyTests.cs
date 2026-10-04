using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Statistics.WebApi.Controllers;
using Sarah.Statistics.WebApi.Data;
using Sarah.Statistics.WebApi.Data.Repositories;
using Sarah.Statistics.WebApi.Services;

namespace Sarah.Statistics.Tests;

public class EnergyTests
{
    private static readonly DateTime T0 = new(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc);

    private static StatisticsDbContext NewDb() =>
        new(new DbContextOptionsBuilder<StatisticsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static EnergySample S(int minutes, double w, double? kwh = null, int device = 1) =>
        new() { DeviceId = device, DeviceName = "Plug " + device, Timestamp = T0.AddMinutes(minutes), PowerW = w, EnergyKwhTotal = kwh };

    private static IConfiguration Config() => new ConfigurationBuilder().Build();

    private static Repository<T> Repo<T>(StatisticsDbContext db) where T : class => new(db);

    private static EnergyQueryService Query(StatisticsDbContext db) =>
        new(Repo<EnergySample>(db), Repo<EnergyAggregate>(db), Config());

    private static EnergyAggregationJob Job(StatisticsDbContext db) =>
        new(Repo<EnergySample>(db), Repo<EnergyAggregate>(db));

    [Fact]
    public void Compute_IntegratesPower_WhenNoMeter()
    {
        var r = EnergyCalculator.Compute(null, [S(0, 1000), S(30, 1000), S(60, 1000)]);
        Assert.Equal(1.0, r!.EnergyKwh, 6);
        Assert.Equal(1000, r.AvgPowerW);
        Assert.Equal(3, r.SampleCount);
    }

    [Fact]
    public void Compute_UsesMeterDifference()
    {
        var r = EnergyCalculator.Compute(S(-10, 0, 5.0), [S(0, 500, 5.2), S(10, 500, 5.5)]);
        Assert.Equal(0.5, r!.EnergyKwh, 6);
    }

    [Fact]
    public void Compute_MeterReset_CountsNewValue()
    {
        var r = EnergyCalculator.Compute(null, [S(0, 0, 10.0), S(10, 0, 0.3)]);
        Assert.Equal(0.3, r!.EnergyKwh, 6);
    }

    [Fact]
    public void Compute_IgnoresLargeGaps_ForPowerIntegration()
    {
        var r = EnergyCalculator.Compute(null, [S(0, 1000), S(180, 1000)]);
        Assert.Equal(0, r!.EnergyKwh);
    }

    [Fact]
    public void Compute_EmptyReturnsNull() => Assert.Null(EnergyCalculator.Compute(null, []));

    [Theory]
    [InlineData(EnergyGranularity.Quarter, 10, 22, 10, 15)]
    [InlineData(EnergyGranularity.Hour, 10, 22, 10, 0)]
    [InlineData(EnergyGranularity.Day, 10, 22, 0, 0)]
    public void BucketStart_AlignsToGranularity(EnergyGranularity g, int h, int m, int eh, int em)
    {
        var start = EnergyCalculator.BucketStart(new DateTime(2026, 3, 10, h, m, 0, DateTimeKind.Utc), g);
        Assert.Equal(new DateTime(2026, 3, 10, eh, em, 0, DateTimeKind.Utc), start);
    }

    [Fact]
    public async Task Store_PersistsValidAndRejectsInvalid()
    {
        using var db = NewDb();
        var store = new EnergySampleStore(Repo<EnergySample>(db));
        Assert.True(await store.StoreAsync(new DeviceEnergyMeasuredMessage { DeviceId = 3, PowerW = 12, MeasuredAt = T0 }));
        Assert.False(await store.StoreAsync(new DeviceEnergyMeasuredMessage { DeviceId = 3, PowerW = -1, MeasuredAt = T0 }));
        Assert.False(await store.StoreAsync(new DeviceEnergyMeasuredMessage { DeviceId = 3, PowerW = double.NaN, MeasuredAt = T0 }));
        Assert.Single(db.EnergySamples);
    }

    [Fact]
    public async Task Aggregation_IsIdempotentAndBuildsAllGranularities()
    {
        using var db = NewDb();
        db.EnergySamples.AddRange(S(0, 1000), S(30, 1000), S(60, 1000));
        await db.SaveChangesAsync();
        var job = Job(db);

        await job.AggregateAsync(T0.AddHours(2), TimeSpan.FromHours(3));
        await job.AggregateAsync(T0.AddHours(2), TimeSpan.FromHours(3));

        var day = db.EnergyAggregates.Single(a => a.Granularity == EnergyGranularity.Day);
        Assert.Equal(1.0, day.EnergyKwh, 6);
        Assert.Equal(2, db.EnergyAggregates.Count(a => a.Granularity == EnergyGranularity.Hour));
    }

    [Fact]
    public async Task Retention_RemovesOldRows()
    {
        using var db = NewDb();
        db.EnergySamples.Add(S(0, 1));
        await db.SaveChangesAsync();
        await Job(db).ApplyRetentionAsync(T0.AddDays(31), 30, 730);
        Assert.Empty(db.EnergySamples);
    }

    [Fact]
    public async Task Query_SummaryDevicesLiveAndTimeseries()
    {
        using var db = NewDb();
        db.EnergySamples.AddRange(S(0, 100, device: 1), S(0, 300, device: 2));
        db.EnergyAggregates.AddRange(
            new EnergyAggregate { DeviceId = 1, DeviceName = "A", Granularity = EnergyGranularity.Quarter, BucketStart = T0, EnergyKwh = 1, AvgPowerW = 100, MaxPowerW = 150, SampleCount = 2 },
            new EnergyAggregate { DeviceId = 2, DeviceName = "B", Granularity = EnergyGranularity.Quarter, BucketStart = T0, EnergyKwh = 3, AvgPowerW = 300, MaxPowerW = 400, SampleCount = 2 },
            new EnergyAggregate { DeviceId = 1, DeviceName = "A", Granularity = EnergyGranularity.Hour, BucketStart = T0, EnergyKwh = 1, AvgPowerW = 100, MaxPowerW = 150, SampleCount = 2 },
            new EnergyAggregate { DeviceId = 2, DeviceName = "B", Granularity = EnergyGranularity.Hour, BucketStart = T0, EnergyKwh = 3, AvgPowerW = 300, MaxPowerW = 400, SampleCount = 2 },
            new EnergyAggregate { DeviceId = 1, DeviceName = "A", Granularity = EnergyGranularity.Hour, BucketStart = T0.AddDays(-1), EnergyKwh = 2, AvgPowerW = 100, MaxPowerW = 150, SampleCount = 2 });
        await db.SaveChangesAsync();
        var q = Query(db);
        var now = T0.AddMinutes(5);

        var summary = await q.GetSummaryAsync(T0.AddHours(-1), T0.AddHours(1), now);
        Assert.Equal(4, summary.TotalEnergyKwh);
        Assert.Equal(400, summary.CurrentPowerW);
        Assert.Equal(1.4m, summary.EstimatedCost);

        var devices = await q.GetDevicesAsync(T0.AddHours(-1), T0.AddHours(1), EnergyGranularity.Hour, now);
        Assert.Equal(2, devices[0].DeviceId);
        Assert.Equal(75, devices[0].SharePercent, 6);
        Assert.Equal(300, devices[0].CurrentPowerW);

        var series = await q.GetTimeseriesAsync(1, T0.AddDays(-2), T0.AddHours(1), EnergyGranularity.Hour);
        Assert.Equal(2, series.Count);
        Assert.True(series[0].BucketStart < series[1].BucketStart);

        Assert.Empty(await q.GetLiveAsync(T0.AddHours(5)));
    }

    [Fact]
    public async Task Summary_UsesSameGranularityPolicyAsDeviceAndTimeseriesEndpoints()
    {
        using var db = NewDb();
        var from = T0;
        var to = from.AddDays(30);
        db.EnergyAggregates.AddRange(
            new EnergyAggregate { DeviceId = 1, Granularity = EnergyGranularity.Hour, BucketStart = from, EnergyKwh = 100, SampleCount = 1 },
            new EnergyAggregate { DeviceId = 1, Granularity = EnergyGranularity.Day, BucketStart = from, EnergyKwh = 2, SampleCount = 1 },
            new EnergyAggregate { DeviceId = 1, Granularity = EnergyGranularity.Day, BucketStart = from.AddDays(1), EnergyKwh = 3, SampleCount = 1 });
        await db.SaveChangesAsync();

        var summary = await Query(db).GetSummaryAsync(from, to, to);
        var devices = await Query(db).GetDevicesAsync(from, to, EnergyQueryService.ResolveGranularity(from, to, null), to);

        Assert.Equal(5, summary.TotalEnergyKwh);
        Assert.Equal(5, devices.Sum(d => d.EnergyKwh));
    }

    [Theory]
    [InlineData(2, EnergyGranularity.Quarter)]
    [InlineData(48, EnergyGranularity.Hour)]
    [InlineData(24 * 30, EnergyGranularity.Day)]
    public void ResolveGranularity_Auto(int hours, EnergyGranularity expected) =>
        Assert.Equal(expected, EnergyQueryService.ResolveGranularity(T0, T0.AddHours(hours), null));

    [Fact]
    public void Trend_NullWithoutPrevious()
    {
        Assert.Null(EnergyQueryService.TrendPercent(5, 0));
        Assert.Equal(50, EnergyQueryService.TrendPercent(3, 2)!.Value, 6);
    }

    [Fact]
    public async Task Controller_RejectsInvalidRange_AndRequiresAuthorization()
    {
        using var db = NewDb();
        var controller = new EnergyStatisticsController(Query(db));
        var result = await controller.GetSummary(T0, T0.AddHours(-1), default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<OkObjectResult>((await controller.GetLive(default)).Result);
        Assert.NotNull(Attribute.GetCustomAttribute(typeof(EnergyStatisticsController), typeof(AuthorizeAttribute)));
    }
}
