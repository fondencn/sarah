using System.Text.Json;
using Sarah.API.BusinessObjects;
using Sarah.Rules.Data.Entities;

namespace Sarah.Rules.Services;

public sealed class AlarmScheduleValidationException(string message) : Exception(message);

public sealed class AlarmScheduleConflictException(string message) : Exception(message);

/// <summary>Validation and recurrence-overlap rules for room temperature schedules.</summary>
public static class RoomTemperatureScheduleRules
{
    private const string ConflictMessage =
        "Für diesen Raum ist zu dieser wiederkehrenden Ortszeit bereits ein Zeitplan vorhanden. Wähle eine andere Zeit oder einen anderen Raum.";
    private const string TemperatureMessage = "Gib eine Zieltemperatur von 8 bis 30 °C ein.";
    private const string RecurrenceMessage = "Raumtemperatur-Zeitpläne müssen wiederkehrend sein.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Validate(AlarmScheduleEntity schedule)
    {
        if (schedule.ContentType != AlarmContentType.TemperatureSchedule)
        {
            return;
        }

        var content = schedule.Content;
        if (content?.RoomId is null)
        {
            throw new AlarmScheduleValidationException("Bitte wähle einen Raum für den Temperaturplan aus.");
        }

        if (content.TargetTemperature is not double target || !double.IsFinite(target) || target is < 8 or > 30)
        {
            throw new AlarmScheduleValidationException(TemperatureMessage);
        }

        if (!schedule.HasRecurrence || string.IsNullOrWhiteSpace(schedule.SerializedRecurrence))
        {
            throw new AlarmScheduleValidationException(RecurrenceMessage);
        }

        try
        {
            var recurrence = JsonSerializer.Deserialize<RecurrenceDefinition>(schedule.SerializedRecurrence, JsonOptions);
            if (recurrence is null
                || recurrence.Interval < 1
                || string.IsNullOrWhiteSpace(recurrence.Freq)
                || !IsSupportedFrequency(recurrence.Freq)
                || recurrence.Dtstart == default
                || recurrence.Until.HasValue)
            {
                throw new AlarmScheduleValidationException("Die Wiederholungseinstellungen sind ungültig; Temperaturpläne benötigen eine Wiederholung ohne Enddatum.");
            }
        }
        catch (JsonException)
        {
            throw new AlarmScheduleValidationException("Die Wiederholungseinstellungen sind ungültig.");
        }
    }

    public static bool HasConflict(
        AlarmScheduleEntity candidate,
        IEnumerable<AlarmScheduleEntity> existingSchedules,
        long? excludedScheduleId = null)
    {
        if (candidate.ContentType != AlarmContentType.TemperatureSchedule || candidate.Content?.RoomId is not long roomId)
        {
            return false;
        }

        var candidateRecurrence = ParseRecurrence(candidate);
        foreach (var existing in existingSchedules)
        {
            if (existing.Id == excludedScheduleId
                || existing.ContentType != AlarmContentType.TemperatureSchedule
                || existing.Content?.RoomId != roomId
                || !existing.HasRecurrence)
            {
                continue;
            }

            var existingRecurrence = ParseRecurrence(existing);
            if (candidateRecurrence.Start.TimeOfDay != existingRecurrence.Start.TimeOfDay)
            {
                continue;
            }

            if (HasCommonOccurrence(candidateRecurrence, existingRecurrence))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetConflictMessage() => ConflictMessage;

    public static bool ShouldApplyTarget(double targetTemperature, double? currentSetpoint, double? outdoorTemperature)
    {
        return !outdoorTemperature.HasValue
            || !double.IsFinite(outdoorTemperature.Value)
            || outdoorTemperature.Value < 20
            || !currentSetpoint.HasValue
            || !double.IsFinite(currentSetpoint.Value)
            || targetTemperature <= currentSetpoint.Value;
    }

    private static RecurrenceDefinition ParseRecurrence(AlarmScheduleEntity alarm)
    {
        try
        {
            var recurrence = JsonSerializer.Deserialize<RecurrenceDefinition>(alarm.SerializedRecurrence!, JsonOptions);
            if (recurrence is null
                || recurrence.Interval < 1
                || string.IsNullOrWhiteSpace(recurrence.Freq)
                || !IsSupportedFrequency(recurrence.Freq))
            {
                throw new AlarmScheduleValidationException("Die Wiederholungseinstellungen sind ungültig.");
            }

            recurrence.Start = NormalizeLocal(recurrence.Dtstart);
            recurrence.UntilLocal = recurrence.Until is null ? null : NormalizeLocal(recurrence.Until.Value);
            return recurrence;
        }
        catch (JsonException)
        {
            throw new AlarmScheduleValidationException("Die Wiederholungseinstellungen sind ungültig.");
        }
    }

    private static bool HasCommonOccurrence(RecurrenceDefinition first, RecurrenceDefinition second)
    {
        // Gregorian recurrence patterns repeat within 400 years. Searching from the
        // later DTSTART covers one full calendar cycle, including leap-day patterns.
        var firstDate = first.Start.Date;
        var secondDate = second.Start.Date;
        var fromDate = firstDate > secondDate ? firstDate : secondDate;
        var throughDate = fromDate.AddYears(400);
        var firstOccurrences = GetOccurrences(first, fromDate, throughDate).ToHashSet();
        return GetOccurrences(second, fromDate, throughDate).Any(firstOccurrences.Contains);
    }

    private static IEnumerable<DateTime> GetOccurrences(RecurrenceDefinition recurrence, DateTime fromDate, DateTime throughDate)
    {
        var frequency = recurrence.Freq!.Trim().ToLowerInvariant();
        if (frequency is "daily" or "monthly" or "yearly")
        {
            var current = recurrence.Start;
            while (current < fromDate)
            {
                current = Advance(current, frequency, recurrence.Interval);
            }

            while (current.Date <= throughDate)
            {
                if (current.Date >= fromDate && (recurrence.UntilLocal is null || current <= recurrence.UntilLocal))
                {
                    yield return current;
                }

                current = Advance(current, frequency, recurrence.Interval);
            }

            yield break;
        }

        if (frequency != "weekly")
        {
            yield break;
        }

        var weekdays = ParseWeekdays(recurrence.Byweekday);
        if (weekdays.Count == 0)
        {
            weekdays.Add(recurrence.Start.DayOfWeek);
        }

        var currentDate = fromDate;
        while (currentDate <= throughDate)
        {
            var weeksSinceStart = (int)Math.Floor((currentDate - recurrence.Start.Date).TotalDays / 7d);
            var occurrence = currentDate.Add(recurrence.Start.TimeOfDay);
            if (currentDate >= recurrence.Start.Date
                && weeksSinceStart >= 0
                && weeksSinceStart % recurrence.Interval == 0
                && weekdays.Contains(currentDate.DayOfWeek)
                && occurrence >= recurrence.Start
                && (recurrence.UntilLocal is null || occurrence <= recurrence.UntilLocal))
            {
                yield return occurrence;
            }

            currentDate = currentDate.AddDays(1);
        }
    }

    private static bool IsSupportedFrequency(string frequency) => frequency.Trim().ToLowerInvariant() is "daily" or "weekly" or "monthly" or "yearly";

    private static DateTime Advance(DateTime occurrence, string frequency, int interval) => frequency switch
    {
        "daily" => occurrence.AddDays(interval),
        "monthly" => occurrence.AddMonths(interval),
        "yearly" => occurrence.AddYears(interval),
        _ => occurrence
    };

    private static HashSet<DayOfWeek> ParseWeekdays(string[]? values)
    {
        var result = new HashSet<DayOfWeek>();
        if (values is null)
        {
            return result;
        }

        foreach (var value in values)
        {
            var day = value.Trim().ToLowerInvariant() switch
            {
                "mo" => DayOfWeek.Monday,
                "tu" => DayOfWeek.Tuesday,
                "we" => DayOfWeek.Wednesday,
                "th" => DayOfWeek.Thursday,
                "fr" => DayOfWeek.Friday,
                "sa" => DayOfWeek.Saturday,
                "su" => DayOfWeek.Sunday,
                _ => (DayOfWeek?)null
            };

            if (day.HasValue)
            {
                result.Add(day.Value);
            }
        }

        return result;
    }

    private static DateTime NormalizeLocal(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value,
        DateTimeKind.Utc => value.ToLocalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Local)
    };

    private sealed class RecurrenceDefinition
    {
        public string? Freq { get; set; }
        public int Interval { get; set; } = 1;
        public string[]? Byweekday { get; set; }
        public DateTime Dtstart { get; set; }
        public DateTime? Until { get; set; }
        public DateTime Start { get; set; }
        public DateTime? UntilLocal { get; set; }
    }
}
