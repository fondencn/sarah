using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Statistics.WebApi.Data;

namespace Sarah.Statistics.WebApi.Services;

/// <summary>Aggregates raw samples into quarter-hour/hour/day buckets and applies retention.</summary>
public class EnergyAggregationJob
{
    private static readonly EnergyGranularity[] Granularities = [EnergyGranularity.Quarter, EnergyGranularity.Hour, EnergyGranularity.Day];

    private readonly StatisticsDbContext _db;

    public EnergyAggregationJob(StatisticsDbContext db)
    {
        _db = db;
    }

    /// <summary>Recomputes all buckets touching the last <paramref name="lookback"/> (aligned to the start of the day).</summary>
    public async Task AggregateAsync(DateTime now, TimeSpan lookback, CancellationToken ct = default)
    {
        var windowStart = EnergyCalculator.BucketStart(now - lookback, EnergyGranularity.Day);
        var loadFrom = windowStart.AddHours(-24);

        var samples = await _db.EnergySamples
            .Where(s => s.Timestamp >= loadFrom)
            .OrderBy(s => s.Timestamp)
            .ToListAsync(ct);

        foreach (var deviceSamples in samples.GroupBy(s => s.DeviceId))
        {
            var ordered = deviceSamples.ToList();
            foreach (var granularity in Granularities)
            {
                var existing = await _db.EnergyAggregates
                    .Where(a => a.DeviceId == deviceSamples.Key && a.Granularity == granularity && a.BucketStart >= windowStart)
                    .ToDictionaryAsync(a => a.BucketStart, ct);

                foreach (var bucket in ordered
                    .Where(s => s.Timestamp >= windowStart)
                    .GroupBy(s => EnergyCalculator.BucketStart(s.Timestamp, granularity)))
                {
                    var previous = ordered.LastOrDefault(s => s.Timestamp < bucket.Key);
                    var result = EnergyCalculator.Compute(previous, bucket.ToList());
                    if (result == null)
                    {
                        continue;
                    }

                    var name = bucket.Last().DeviceName;
                    if (!existing.TryGetValue(bucket.Key, out var aggregate))
                    {
                        aggregate = new EnergyAggregate
                        {
                            DeviceId = deviceSamples.Key,
                            Granularity = granularity,
                            BucketStart = bucket.Key
                        };
                        _db.EnergyAggregates.Add(aggregate);
                    }

                    aggregate.DeviceName = name;
                    aggregate.AvgPowerW = result.AvgPowerW;
                    aggregate.MaxPowerW = result.MaxPowerW;
                    aggregate.EnergyKwh = result.EnergyKwh;
                    aggregate.SampleCount = result.SampleCount;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task ApplyRetentionAsync(DateTime now, int rawRetentionDays, int aggregateRetentionDays, CancellationToken ct = default)
    {
        var rawCutoff = now.AddDays(-rawRetentionDays);
        var aggregateCutoff = now.AddDays(-aggregateRetentionDays);
        _db.EnergySamples.RemoveRange(_db.EnergySamples.Where(s => s.Timestamp < rawCutoff));
        _db.EnergyAggregates.RemoveRange(_db.EnergyAggregates.Where(a => a.BucketStart < aggregateCutoff));
        await _db.SaveChangesAsync(ct);
    }
}

public class EnergyAggregationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnergyAggregationService> _logger;

    public EnergyAggregationService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<EnergyAggregationService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastRetention = DateTime.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var job = new EnergyAggregationJob(scope.ServiceProvider.GetRequiredService<StatisticsDbContext>());
                var now = DateTime.UtcNow;
                await job.AggregateAsync(now, TimeSpan.FromHours(3), stoppingToken);

                if (now - lastRetention > TimeSpan.FromHours(6))
                {
                    await job.ApplyRetentionAsync(
                        now,
                        _configuration.GetValue("Statistics:RawRetentionDays", 30),
                        _configuration.GetValue("Statistics:AggregateRetentionDays", 730),
                        stoppingToken);
                    lastRetention = now;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Energy aggregation failed");
            }

            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
