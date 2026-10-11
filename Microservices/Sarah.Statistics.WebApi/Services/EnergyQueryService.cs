using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Statistics.WebApi.Data;
using Sarah.Statistics.WebApi.Data.Repositories;

namespace Sarah.Statistics.WebApi.Services;

public class EnergyQueryService
{
    public static readonly TimeSpan LiveWindow = TimeSpan.FromMinutes(30);

    private readonly IRepository<EnergySample> _samples;
    private readonly IRepository<EnergyAggregate> _aggregates;
    private readonly decimal _pricePerKwh;

    public EnergyQueryService(IRepository<EnergySample> samples, IRepository<EnergyAggregate> aggregates, IConfiguration configuration)
    {
        _samples = samples;
        _aggregates = aggregates;
        _pricePerKwh = configuration.GetValue("Statistics:PricePerKwh", 0.35m);
    }

    public static EnergyGranularity ResolveGranularity(DateTime from, DateTime to, EnergyGranularity? requested)
    {
        if (requested.HasValue)
        {
            return requested.Value;
        }
        var span = to - from;
        if (span <= TimeSpan.FromHours(36)) return EnergyGranularity.Quarter;
        if (span <= TimeSpan.FromDays(8)) return EnergyGranularity.Hour;
        return EnergyGranularity.Day;
    }

    public static double? TrendPercent(double current, double previous) =>
        previous > 0 ? (current - previous) / previous * 100.0 : null;

    public async Task<List<EnergyLiveDeviceDto>> GetLiveAsync(DateTime now, CancellationToken ct = default)
    {
        var since = now - LiveWindow;
        var recent = await _samples.Query().Where(s => s.Timestamp >= since).ToListAsync(ct);
        return recent
            .GroupBy(s => s.DeviceId)
            .Select(g => g.OrderByDescending(s => s.Timestamp).First())
            .OrderByDescending(s => s.PowerW)
            .Select(s => new EnergyLiveDeviceDto { DeviceId = s.DeviceId, DeviceName = s.DeviceName, PowerW = s.PowerW, Timestamp = s.Timestamp })
            .ToList();
    }

    public async Task<List<EnergyDeviceStatisticsDto>> GetDevicesAsync(DateTime from, DateTime to, EnergyGranularity granularity, DateTime now, CancellationToken ct = default)
    {
        var aggregates = await _aggregates.Query()
            .Where(a => a.Granularity == granularity &&
                        a.BucketStart >= AlignBucketStart(from, granularity) &&
                        a.BucketStart < AlignBucketStart(to, granularity))
            .ToListAsync(ct);
        var live = (await GetLiveAsync(now, ct)).ToDictionary(l => l.DeviceId);

        var devices = aggregates.GroupBy(a => a.DeviceId).Select(g =>
        {
            var samples = g.Sum(a => a.SampleCount);
            var weight = g.Sum(a => a.CoverageHours < 0 ? a.SampleCount : a.CoverageHours);
            return new EnergyDeviceStatisticsDto
            {
                DeviceId = g.Key,
                DeviceName = g.OrderByDescending(a => a.BucketStart).First().DeviceName,
                EnergyKwh = g.Sum(a => a.EnergyKwh),
                MaxPowerW = g.Max(a => a.MaxPowerW),
                AvgPowerW = weight > 0
                    ? g.Sum(a => a.AvgPowerW * (a.CoverageHours < 0 ? a.SampleCount : a.CoverageHours)) / weight
                    : 0,
                CurrentPowerW = live.TryGetValue(g.Key, out var l) ? l.PowerW : 0
            };
        }).OrderByDescending(d => d.EnergyKwh).ToList();

        var total = devices.Sum(d => d.EnergyKwh);
        foreach (var d in devices)
        {
            d.SharePercent = total > 0 ? d.EnergyKwh / total * 100.0 : 0;
        }
        return devices;
    }

    public async Task<List<EnergyTimeseriesPointDto>> GetTimeseriesAsync(int deviceId, DateTime from, DateTime to, EnergyGranularity granularity, CancellationToken ct = default)
    {
        return await _aggregates.Query()
            .Where(a => a.DeviceId == deviceId && a.Granularity == granularity &&
                        a.BucketStart >= AlignBucketStart(from, granularity) &&
                        a.BucketStart < AlignBucketStart(to, granularity))
            .OrderBy(a => a.BucketStart)
            .Select(a => new EnergyTimeseriesPointDto { BucketStart = a.BucketStart, AvgPowerW = a.AvgPowerW, MaxPowerW = a.MaxPowerW, EnergyKwh = a.EnergyKwh })
            .ToListAsync(ct);
    }

    public async Task<EnergySummaryDto> GetSummaryAsync(DateTime from, DateTime to, DateTime now, CancellationToken ct = default)
    {
        var granularity = ResolveGranularity(from, to, null);
        var currentFrom = AlignBucketStart(from, granularity);
        var currentTo = AlignBucketStart(to, granularity);
        var span = currentTo - currentFrom;

        var current = await SumAsync(currentFrom, currentTo, granularity, ct);
        var previous = await SumAsync(currentFrom - span, currentFrom, granularity, ct);
        var live = await GetLiveAsync(now, ct);

        return new EnergySummaryDto
        {
            From = from,
            To = to,
            CurrentPowerW = live.Sum(l => l.PowerW),
            TotalEnergyKwh = current.Sum,
            PreviousEnergyKwh = previous.Sum,
            TrendPercent = TrendPercent(current.Sum, previous.Sum),
            PricePerKwh = _pricePerKwh,
            EstimatedCost = Math.Round((decimal)current.Sum * _pricePerKwh, 2),
            DeviceCount = current.Devices
        };
    }

    private async Task<(double Sum, int Devices)> SumAsync(DateTime from, DateTime to, EnergyGranularity granularity, CancellationToken ct)
    {
        var rows = await _aggregates.Query()
            .Where(a => a.Granularity == granularity &&
                        a.BucketStart >= AlignBucketStart(from, granularity) &&
                        a.BucketStart < AlignBucketStart(to, granularity))
            .Select(a => new { a.DeviceId, a.EnergyKwh })
            .ToListAsync(ct);
        return (rows.Sum(r => r.EnergyKwh), rows.Select(r => r.DeviceId).Distinct().Count());
    }

    private static DateTime AlignBucketStart(DateTime timestamp, EnergyGranularity granularity) =>
        EnergyCalculator.BucketStart(timestamp, granularity);
}
