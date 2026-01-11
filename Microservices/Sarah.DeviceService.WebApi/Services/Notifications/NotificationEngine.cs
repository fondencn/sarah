using Sarah.API.Interfaces;

namespace Sarah.DeviceService.Notifications
{
    public class NotificationEngine
    {
        /// <summary>
        /// Sprachausgabe an alle Lautsprecher im System senden
        /// </summary>
        public const string BroadcastAllSpeakers = "BroadcastAllSpeakers";
        /// <summary>
        /// Lautsprecher im Arbeitszimmer
        /// </summary>
        public const string Speaker1 = "Speaker1";
        /// <summary>
        /// Respeaker Core v2
        /// </summary>
        public const string Speaker2 = "Speaker2";
        /// <summary>
        /// Tablet im Wohnzimmer
        /// </summary>
        public const string Speaker3 = "Speaker3";

        /// <summary>
        /// ctor
        /// </summary>
        public NotificationEngine(IEmailNotifier email, IVoiceNotifier voice)
        {
            this.Email = email;
            this.Voice = voice;
        }

        /// <summary>
        /// E-Mail Notifier 
        /// </summary>
        public IEmailNotifier Email { get; }
        /// <summary>
        /// Voice Notifier
        /// </summary>
        public IVoiceNotifier Voice { get; }
    }
}
