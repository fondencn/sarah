using System.Reflection;
using System.Text.Json;
using Sarah.Rules.Services;

namespace Sarah.Rules.Tests;

public class AlarmScheduleRecurrenceTests
{
    [Fact]
    public void GetNextOccurrenceLocal_WeeklyRecurrenceSkipsUnselectedFutureDtstartDay()
    {
        var recurrenceType = typeof(AlarmScheduleService).GetNestedType(
            "AlarmRecurrenceDefinition",
            BindingFlags.NonPublic)!;
        var recurrence = JsonSerializer.Deserialize(
            """
            {
              "freq": "weekly",
              "interval": 1,
              "byweekday": ["tu"],
              "dtstart": "2026-10-12T15:00:00"
            }
            """,
            recurrenceType,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var getNextOccurrence = recurrenceType.GetMethod("GetNextOccurrenceLocal")!;

        var actual = (DateTime?)getNextOccurrence.Invoke(
            recurrence,
            [new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Local)]);

        Assert.Equal(new DateTime(2026, 10, 13, 15, 0, 0, DateTimeKind.Local), actual);
    }
}
