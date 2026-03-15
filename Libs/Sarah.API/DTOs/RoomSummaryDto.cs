namespace Sarah.API.BusinessObjects.DTOs
{
    public class RoomSummaryDto
    {
        public long RoomId { get; set; }
        public double? AverageTemperature { get; set; }
        public bool AnyDoorOpen { get; set; }
        public bool AnyPresence { get; set; }
    }
}
