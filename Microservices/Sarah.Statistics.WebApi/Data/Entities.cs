using Sarah.API.BusinessObjects.DTOs;

namespace Sarah.Statistics.WebApi.Data;

public class EnergySample
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public DateTime Timestamp { get; set; }
    public double PowerW { get; set; }
    public double? EnergyKwhTotal { get; set; }
}

public class EnergyAggregate
{
    public long Id { get; set; }
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public EnergyGranularity Granularity { get; set; }
    public DateTime BucketStart { get; set; }
    public double AvgPowerW { get; set; }
    public double MaxPowerW { get; set; }
    public double EnergyKwh { get; set; }
    public int SampleCount { get; set; }
}
