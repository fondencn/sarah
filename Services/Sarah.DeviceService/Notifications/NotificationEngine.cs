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



        #region singleton pattern
        private static NotificationEngine _Instance;
        public static NotificationEngine Instance
        {
            get
            {
                if(_Instance == null)
                {
                    _Instance = new NotificationEngine();
                }
                return _Instance;
            }
        }

        private NotificationEngine()
        {
            this.Email = new DieRooterEmailNotifier();
        }
        #endregion

        public IEmailNotifier Email { get; set; }
        public IVoiceNotifier Voice { get; set; }

        public IWeatherProvider Weather { get; set; }
        public IDeseaseStatsProvider Deseases { get; set; }
        public IFerienInfoProvider Ferien { get; set; }
        public IGeoService Geo { get; set; }
    }
}
