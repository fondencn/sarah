using Sarah.API.BusinessObjects;

namespace Sarah.API.BusinessObjects.SpeakerEvents
{
    public class SayRequest
    {
        public string Text { get; set; } = "";
        public string Hostname { get; set; } = "";
        public SpeechVolume Volume { get; set; } = SpeechVolume.Normal;
    }
}
