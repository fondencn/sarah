using Sarah.API.BusinessObjects;

namespace Sarah.API.Interfaces
{
    public interface IVoiceNotifier
    {
        void Say(string text);
        void Say(string text, string hostname);
        void Say(string text, string hostname, SpeechVolume volume);
        void StartPlayAudio(string audioFileName, string hostname);
        void StopPlayAudio(string hostname);
    }
}
