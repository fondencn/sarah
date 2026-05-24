using System.Text.Json.Serialization;

namespace Sarah.API.BusinessObjects
{
    public enum AlarmContentType
    {
        Text = 0,
        TemperatureSchedule = 1
    }

    public sealed class AlarmContentDto
    {
        public AlarmContentType Type { get; set; }

        public string? Text { get; set; }

        public string? TargetSpeaker { get; set; }

        public int? Volume { get; set; }

        public long? RoomId { get; set; }

        public double? TargetTemperature { get; set; }

        [JsonIgnore]
        public string DisplayText
        {
            get
            {
                if (Type == AlarmContentType.TemperatureSchedule)
                {
                    return TargetTemperature.HasValue
                        ? $"Temperatur {TargetTemperature:0.#}°C"
                        : "Temperaturschaltung";
                }

                return Text ?? string.Empty;
            }
        }
    }
}