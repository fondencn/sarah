using Sarah.DeviceService.WebApi.Controllers;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class RoomSummaryCalculationTests
{
    [Fact]
    public void CalculateAverageRoomTemperature_ExcludesZeroReadings()
    {
        var temperatures = new[] { 0f, 21.5f, 0f, 19.0f };

        var result = DevicesController.CalculateAverageRoomTemperature(temperatures);

        // (21.5 + 19.0) / 2 = 20.25, Math.Round to 1 decimal with banker's rounding → 20.2
        Assert.Equal(20.2, result);
    }

    [Fact]
    public void CalculateAverageRoomTemperature_AllZeros_ReturnsNull()
    {
        var temperatures = new[] { 0f, 0f, 0f };

        var result = DevicesController.CalculateAverageRoomTemperature(temperatures);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateAverageRoomTemperature_EmptyList_ReturnsNull()
    {
        var result = DevicesController.CalculateAverageRoomTemperature([]);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateAverageRoomTemperature_NoZeros_AveragesAllReadings()
    {
        var temperatures = new[] { 20.0f, 22.0f };

        var result = DevicesController.CalculateAverageRoomTemperature(temperatures);

        Assert.Equal(21.0, result);
    }

    [Fact]
    public void CalculateAverageRoomTemperature_SingleNonZeroReading_ReturnsThatValue()
    {
        var temperatures = new[] { 0f, 18.7f };

        var result = DevicesController.CalculateAverageRoomTemperature(temperatures);

        Assert.Equal(18.7, result);
    }

    [Fact]
    public void CalculateAverageRoomTemperature_RoundsToOneDecimalPlace()
    {
        var temperatures = new[] { 20.1f, 20.2f, 20.3f };

        var result = DevicesController.CalculateAverageRoomTemperature(temperatures);

        Assert.Equal(20.2, result);
    }
}
