using Sarah.Messaging.RabbitMQ;

namespace Sarah.Messaging.RabbitMQ.Messages;

public class OutDoorTemperatureChangedEventMessage : AbstractMessage
{
    public OutDoorTemperatureChangedEventMessage(double newVal) 
    {
        Topic = "weather.outdoortemperature";
        NewValue = newVal;
    }

    public double NewValue { get; set; }
}

public class WeatherWarningEventMessage : AbstractMessage
{
    public WeatherWarningEventMessage(string newVal) 
    {
        Topic = "weather.warning";
        NewValue = newVal;
    }

    public string NewValue { get; set; }
}