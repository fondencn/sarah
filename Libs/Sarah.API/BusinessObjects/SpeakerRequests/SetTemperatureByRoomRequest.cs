namespace Sarah.API.BusinessObjects.SpeakerRequests
{
    public class SetTemperatureByRoomRequest
    {
        public string RoomName { get; set; } = string.Empty;
        public float Temperature { get; set; }
    }
}
