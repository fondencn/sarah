using Sarah.API.BusinessObjects.DTOs;
using Sarah.Statistics.WebApi.Data;

namespace Sarah.Statistics.WebApi.Services;

public record EnergyBucketResult(double EnergyKwh, double AvgPowerW, double MaxPowerW, int SampleCount);

public static class EnergyCalculator
{
    /// <summary>Maximum gap between two samples for which power is integrated.</summary>
    public static readonly TimeSpan MaxIntegrationGap = TimeSpan.FromHours(1);

    public static DateTime BucketStart(DateTime timestamp, EnergyGranularity granularity)
    {
        var utc = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        return granularity switch
        {
            EnergyGranularity.Quarter => new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute / 15 * 15, 0, DateTimeKind.Utc),
            EnergyGranularity.Hour => new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc),
            _ => new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    public static DateTime BucketEnd(DateTime bucketStart, EnergyGranularity granularity) => granularity switch
    {
        EnergyGranularity.Quarter => bucketStart.AddMinutes(15),
        EnergyGranularity.Hour => bucketStart.AddHours(1),
        _ => bucketStart.AddDays(1)
    };

    /// <summary>
    /// Computes the consumption of a bucket. Each interval between two consecutive samples belongs to the bucket
    /// containing its end. <paramref name="previous"/> is the last sample before the bucket (may be null).
    /// Uses the meter difference when both samples carry a meter reading (a decreasing meter is treated as reset),
    /// otherwise integrates the power (trapezoid rule) over intervals shorter than <see cref="MaxIntegrationGap"/>.
    /// </summary>
    public static EnergyBucketResult? Compute(EnergySample? previous, IReadOnlyList<EnergySample> samples)
    {
        if (samples.Count == 0)
        {
            return null;
        }

        double energy = 0;
        var prev = previous;
        foreach (var current in samples)
        {
            if (prev != null)
            {
                if (prev.EnergyKwhTotal.HasValue && current.EnergyKwhTotal.HasValue)
                {
                    var diff = current.EnergyKwhTotal.Value - prev.EnergyKwhTotal.Value;
                    energy += diff >= 0 ? diff : current.EnergyKwhTotal.Value;
                }
                else
                {
                    var dt = current.Timestamp - prev.Timestamp;
                    if (dt > TimeSpan.Zero && dt <= MaxIntegrationGap)
                    {
                        energy += (prev.PowerW + current.PowerW) / 2.0 * dt.TotalHours / 1000.0;
                    }
                }
            }
            prev = current;
        }

        return new EnergyBucketResult(energy, samples.Average(s => s.PowerW), samples.Max(s => s.PowerW), samples.Count);
    }
}
