namespace Sarah.API.BusinessObjects.DTOs
{
    public class TrackerDto
    {
        public byte Id { get; set; }
        public string? Name { get; set; }

        public PositionDto? Position { get; set; }
        public float? BatteryLevel { get; set; }
    }
}
