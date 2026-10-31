using Sarah.API.BusinessObjects;
using Sarah.Rules.Data.Entities;
using Sarah.Rules.Services;

namespace Sarah.Rules.Tests;

public class RoomTemperatureScheduleRulesTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(21)]
    [InlineData(30)]
    public void Validate_AcceptsTemperatureWithinInclusiveRange(double temperature)
    {
        var schedule = CreateSchedule(roomId: 10, temperature, "weekly", ["mo"], "2026-10-12T07:00:00Z");

        RoomTemperatureScheduleRules.Validate(schedule);
    }

    [Theory]
    [InlineData(7.99)]
    [InlineData(30.01)]
    public void Validate_RejectsTemperatureOutsideRange(double temperature)
    {
        var schedule = CreateSchedule(roomId: 10, temperature, "weekly", ["mo"], "2026-10-12T07:00:00Z");

        var exception = Assert.Throws<AlarmScheduleValidationException>(() => RoomTemperatureScheduleRules.Validate(schedule));

        Assert.Contains("8 bis 30", exception.Message);
    }

    [Fact]
    public void Validate_RequiresRecurringRoomSchedule()
    {
        var schedule = CreateSchedule(roomId: 10, 21, "weekly", ["mo"], "2026-10-12T07:00:00Z");
        schedule.HasRecurrence = false;

        Assert.Throws<AlarmScheduleValidationException>(() => RoomTemperatureScheduleRules.Validate(schedule));
    }

    [Fact]
    public void Validate_RejectsEndDatesForTemperatureSchedules()
    {
        var schedule = CreateSchedule(roomId: 10, 21, "weekly", ["mo"], "2026-10-12T07:00:00Z");
        schedule.SerializedRecurrence = "{\"freq\":\"weekly\",\"interval\":1,\"byweekday\":[\"mo\"],\"dtstart\":\"2026-10-12T07:00:00Z\",\"until\":\"2027-01-01T00:00:00Z\"}";

        Assert.Throws<AlarmScheduleValidationException>(() => RoomTemperatureScheduleRules.Validate(schedule));
    }

    [Fact]
    public void HasConflict_RejectsSharedWeeklyOccurrenceForSameRoomAndTime()
    {
        var existing = CreateSchedule(1, 10, 21, "weekly", ["mo"], "2026-10-12T07:00:00Z");
        var candidate = CreateSchedule(2, 10, 22, "daily", [], "2026-10-12T09:00:00+02:00");

        Assert.True(RoomTemperatureScheduleRules.HasConflict(candidate, [existing]));
    }

    [Fact]
    public void HasConflict_AllowsDifferentRoomsOrDifferentTimes()
    {
        var existing = CreateSchedule(1, 10, 21, "weekly", ["mo"], "2026-10-12T07:00:00Z");
        var otherRoom = CreateSchedule(2, 11, 22, "weekly", ["mo"], "2026-10-12T07:00:00Z");
        var otherTime = CreateSchedule(3, 10, 22, "weekly", ["mo"], "2026-10-12T08:00:00Z");

        Assert.False(RoomTemperatureScheduleRules.HasConflict(otherRoom, [existing]));
        Assert.False(RoomTemperatureScheduleRules.HasConflict(otherTime, [existing]));
    }

    [Fact]
    public void HasConflict_RejectsSameStoredTimeEvenForDisjointWeeklyDays()
    {
        var existing = CreateSchedule(1, 10, 21, "weekly", ["mo"], "2026-10-12T07:00:00Z");
        var candidate = CreateSchedule(2, 10, 22, "weekly", ["tu"], "2026-10-13T07:00:00Z");

        Assert.True(RoomTemperatureScheduleRules.HasConflict(candidate, [existing]));
    }

    [Fact]
    public void HasConflict_ComparesStoredClockWithoutTimezoneConversion()
    {
        var existing = CreateSchedule(1, 10, 21, "weekly", ["mo"], "2026-10-12T07:00:00");
        var sameStoredTimeWithOffset = CreateSchedule(2, 10, 22, "weekly", ["tu"], "2026-10-13T07:00:00+05:00");

        Assert.True(RoomTemperatureScheduleRules.HasConflict(sameStoredTimeWithOffset, [existing]));
    }

    [Fact]
    public void GetConflictMessage_ExplainsRoomAndTimeRestriction()
    {
        var message = RoomTemperatureScheduleRules.GetConflictMessage();

        Assert.Contains("Raum", message);
        Assert.Contains("Uhrzeit", message);
    }

    [Fact]
    public void HasConflict_ExcludesCurrentScheduleOnUpdate()
    {
        var existing = CreateSchedule(1, 10, 21, "weekly", ["mo"], "2026-10-12T07:00:00Z");
        var candidate = CreateSchedule(1, 10, 22, "weekly", ["mo"], "2026-10-12T07:00:00Z");

        Assert.False(RoomTemperatureScheduleRules.HasConflict(candidate, [existing], excludedScheduleId: 1));
    }

    [Theory]
    [InlineData(22d, 20d, 20d, false)]
    [InlineData(21d, 20d, 20d, false)]
    [InlineData(22d, 22d, 25d, true)]
    [InlineData(19d, 20d, 25d, true)]
    [InlineData(22d, 20d, 19.9d, true)]
    [InlineData(22d, 20d, null, true)]
    [InlineData(22d, null, 25d, true)]
    public void ShouldApplyTarget_OnlySuppressesHotOutdoorIncreases(
        double target,
        double? currentSetpoint,
        double? outdoorTemperature,
        bool expected)
    {
        Assert.Equal(expected, RoomTemperatureScheduleRules.ShouldApplyTarget(target, currentSetpoint, outdoorTemperature));
    }

    private static AlarmScheduleEntity CreateSchedule(long? roomId, double temperature, string frequency, string[] weekdays, string dtstart)
        => CreateSchedule(0, roomId, temperature, frequency, weekdays, dtstart);

    private static AlarmScheduleEntity CreateSchedule(long id, long? roomId, double temperature, string frequency, string[] weekdays, string dtstart)
    {
        var schedule = new AlarmScheduleEntity { Id = id, AlarmTime = DateTime.Parse(dtstart).ToUniversalTime(), HasRecurrence = true };
        schedule.SetTemperatureContent(roomId, temperature);
        schedule.SerializedRecurrence = System.Text.Json.JsonSerializer.Serialize(new
        {
            freq = frequency,
            interval = 1,
            byweekday = weekdays,
            dtstart
        });
        return schedule;
    }
}
