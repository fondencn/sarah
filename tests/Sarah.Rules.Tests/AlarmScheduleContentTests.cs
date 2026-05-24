using Sarah.API.BusinessObjects;
using Sarah.Rules.Data.Entities;
using Sarah.Rules.WebApi.Options;

namespace Sarah.Rules.Tests;

public class AlarmScheduleContentTests
{
    [Fact]
    public void SetTextContent_RoundTripsThroughJson()
    {
        var alarm = new AlarmScheduleEntity();

        alarm.SetTextContent("Guten Morgen", "speaker-1", SpeechVolume.Normal);

        Assert.Equal(AlarmContentType.Text, alarm.ContentType);
        Assert.NotNull(alarm.ContentJson);
        Assert.Equal("Guten Morgen", alarm.Text);
        Assert.Equal("Guten Morgen", alarm.Content?.Text);
        Assert.Equal("speaker-1", alarm.Content?.TargetSpeaker);
        Assert.Equal((int)SpeechVolume.Normal, alarm.Content?.Volume);
        Assert.Equal("Guten Morgen", alarm.GetDisplayText());
    }

    [Fact]
    public void SetTemperatureContent_RoundTripsThroughJson()
    {
        var alarm = new AlarmScheduleEntity();

        alarm.SetTemperatureContent(roomId: 12, targetTemperature: 21.5);

        Assert.Equal(AlarmContentType.TemperatureSchedule, alarm.ContentType);
        Assert.NotNull(alarm.ContentJson);
        Assert.Equal(12, alarm.Content?.RoomId);
        Assert.Equal(21.5, alarm.Content?.TargetTemperature);
        Assert.Equal("Temperatur 21.5°C", alarm.GetDisplayText());
    }

    [Theory]
    [InlineData(false, 6, 2026, 7, 15, false)]
    [InlineData(true, 5, 2026, 7, 15, true)]
    [InlineData(true, 11, 2026, 7, 15, false)]
    public void IsSummer_UsesConfiguredMonthRange(
        bool enabled,
        int summerStartMonth,
        int year,
        int month,
        int day,
        bool expected)
    {
        var options = new AlarmExecutionOptions
        {
            SuppressTemperatureAlarmsDuringSummer = enabled,
            SummerStartMonth = summerStartMonth,
            SummerEndMonth = 9
        };

        var now = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Local);

        Assert.Equal(expected, options.IsSummer(now));
    }
}