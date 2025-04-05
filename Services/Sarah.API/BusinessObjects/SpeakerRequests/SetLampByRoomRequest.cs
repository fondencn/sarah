namespace Sarah.API.BusinessObjects.SpeakerRequests
{
    public class SetLampByRoomRequest
    {
        public string RoomName { get; set; }= string.Empty;
        public string LampName { get; set; }= string.Empty;
        public bool On { get; set; } = false;
    }
}
