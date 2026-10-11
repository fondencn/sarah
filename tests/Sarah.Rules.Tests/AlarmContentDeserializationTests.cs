using Sarah.API.BusinessObjects;
using Sarah.Rules;

namespace Sarah.Rules.Tests;

public class AlarmContentDeserializationTests
{
    [Fact]
    public void DeserializeAlarmContent_ParsesExecutionJsonWithStringEnum()
    {
        const string executionJson = "{\"type\":\"temperatureSchedule\",\"roomId\":12,\"targetTemperature\":21.5}";

        var content = RuleService.DeserializeAlarmContent(executionJson);

        Assert.NotNull(content);
        Assert.Equal(AlarmContentType.TemperatureSchedule, content.Type);
        Assert.Equal(12L, content.RoomId);
        Assert.Equal(21.5, content.TargetTemperature);
    }
}
