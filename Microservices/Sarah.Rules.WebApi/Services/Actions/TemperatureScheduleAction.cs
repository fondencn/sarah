using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Rules.Services;
using Sarah.ServiceClients;
using Microsoft.Extensions.DependencyInjection;

namespace Sarah.Rules.Services.Actions;

public sealed class TemperatureScheduleAction
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TemperatureScheduleAction> _logger;
    private static readonly System.Text.Json.JsonSerializerOptions AlarmContentJsonOptions = CreateAlarmContentJsonOptions();

    public TemperatureScheduleAction(IServiceScopeFactory scopeFactory, ILogger<TemperatureScheduleAction> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(AlarmTriggeredMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.ContentJson))
        {
            _logger.LogWarning("Temperature schedule {AlarmScheduleId} has no content", message.AlarmScheduleId);
            return;
        }

        Sarah.API.BusinessObjects.AlarmContentDto? content;
        try
        {
            content = DeserializeAlarmContent(message.ContentJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse temperature schedule {AlarmScheduleId}", message.AlarmScheduleId);
            return;
        }

        if (content?.RoomId is not long roomId || content.TargetTemperature is not double targetTemperature)
        {
            _logger.LogWarning("Temperature schedule {AlarmScheduleId} has incomplete content", message.AlarmScheduleId);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var deviceClient = scope.ServiceProvider.GetRequiredService<DeviceServiceClient>();
            var outdoorTemperature = scope.ServiceProvider
                .GetRequiredService<MessageBasedWeatherProvider>()
                .OutdoorTemperatureIfAvailable;
            var thermostats = (await deviceClient.GetAllDevicesAsync())
                .Where(device => device.RoomId == roomId && device.Thermostat != null)
                .ToList();

            foreach (var thermostat in thermostats)
            {
                var currentSetpoint = thermostat.Thermostat?.TemperatureSetpoint;
                if (!RoomTemperatureScheduleRules.ShouldApplyTarget(
                    targetTemperature,
                    currentSetpoint,
                    outdoorTemperature))
                {
                    _logger.LogInformation(
                        "Temperature increase for room {RoomId} suppressed at outdoor temperature {OutdoorTemperature}°C",
                        roomId,
                        outdoorTemperature);
                    continue;
                }

                try
                {
                    await deviceClient.SetThermostatTemperatureAsync(thermostat.Id, (float)targetTemperature);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to apply temperature schedule {AlarmScheduleId} to thermostat {ThermostatId}",
                        message.AlarmScheduleId,
                        thermostat.Id);
                }
            }

            if (thermostats.Count == 0)
            {
                _logger.LogWarning("No thermostats found for temperature schedule room {RoomId}", roomId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute temperature schedule {AlarmScheduleId}", message.AlarmScheduleId);
        }
    }

    internal static Sarah.API.BusinessObjects.AlarmContentDto? DeserializeAlarmContent(string contentJson)
        => System.Text.Json.JsonSerializer.Deserialize<Sarah.API.BusinessObjects.AlarmContentDto>(contentJson, AlarmContentJsonOptions);

    private static System.Text.Json.JsonSerializerOptions CreateAlarmContentJsonOptions()
    {
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        return options;
    }
}
