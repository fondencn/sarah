using System;
using System.Collections.Generic;

namespace Sarah.API.BusinessObjects.DTOs
{
    public enum EnergyGranularity
    {
        Quarter = 0,
        Hour = 1,
        Day = 2
    }

    public class EnergySummaryDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public double CurrentPowerW { get; set; }
        public double TotalEnergyKwh { get; set; }
        public double PreviousEnergyKwh { get; set; }
        public double? TrendPercent { get; set; }
        public decimal PricePerKwh { get; set; }
        public decimal EstimatedCost { get; set; }
        public int DeviceCount { get; set; }
    }

    public class EnergyDeviceStatisticsDto
    {
        public int DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public double CurrentPowerW { get; set; }
        public double AvgPowerW { get; set; }
        public double MaxPowerW { get; set; }
        public double EnergyKwh { get; set; }
        public double SharePercent { get; set; }
    }

    public class EnergyTimeseriesPointDto
    {
        public DateTime BucketStart { get; set; }
        public double AvgPowerW { get; set; }
        public double MaxPowerW { get; set; }
        public double EnergyKwh { get; set; }
    }

    public class EnergyLiveDeviceDto
    {
        public int DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public double PowerW { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
