using Sarah.API.Interfaces;
using System;
using System.Runtime.CompilerServices;

namespace Sarah.API.BusinessObjects
{
    public abstract class NetworkEvent
    {

        /// <summary>
        /// ZWave Node ID des Gerätes, welches das Ereignis ausgelöst hat
        /// </summary>
        public byte SourceNodeId { get; set;}

        /// <summary>
        /// Zeitpunkt, zu welchem Das Ereignis ausgelöst wurde
        /// </summary>
        public DateTime CreationDate { get;  set;}

        /// <summary>
        /// Name der veränderten Eigenschaft
        /// </summary>
        public string Property { get;  set;}

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newVal"></param>
        public NetworkEvent(byte source, [CallerMemberName] string ?caller = null)
        {
            this.SourceNodeId = source;
            this.CreationDate = DateTime.Now;
            this.Property = caller ?? "NetworkEvent";
        }

        public NetworkEvent() : this(0, null) {}
    }

    /// <summary>
    /// Abstrakte Darstellung eines Netzwerkereignisses aus dem ZWave Netzwerk
    /// </summary>
    /// <typeparam name="TValue">Typ des Wertes</typeparam>
    public  class NetworkEvent<TValue> : NetworkEvent
    {

        /// <summary>
        /// Der neue Wert
        /// </summary>
        public TValue NewValue { get; set;}


        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newVal"></param>
        public NetworkEvent(byte source, TValue newVal, [CallerMemberName] string? caller = null) : base(source, caller ?? "NetworkEvent")
        {
            this.SourceNodeId = source;
            this.CreationDate = DateTime.Now;
            this.Property = caller ?? "NetworkEvent";
            this.NewValue = newVal;
        }

        /// <summary>
        /// ctor 
        /// </summary>
        public NetworkEvent() : this(0, default!, "NetworkEvent") {}
    }

    /// <summary>
    /// Event wird ausgelöst, wenn ein HW-Button gedrückt wird
    /// </summary>
    public class ClickedEvent : NetworkEvent
    {
        public byte SceneId { get;  }
        public ClickedEvent(byte source, byte sceneId, [CallerMemberName] string? caller = null) : base(source, caller?? "ClickedEvent")
        {
            this.SceneId = sceneId;
        }
    }

    /// <summary>
    /// Event der ausgelöst wird, wenn ein Timerereignise eintritt
    /// </summary>
    public class TimerEvent : NetworkEvent
    {
        public TimerEvent(byte source, [CallerMemberName] string? caller = null) : base(source, caller?? "TimerEvent")
        {
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn eine Person zu hause anwesend oder abwesend gemeldet wird
    /// </summary>
    public class PersonAvailabilityEvent : NetworkEvent
    {
        public long Id_Person { get; private set; }
        public bool IsAvailable { get; private set; }
        public string PersonName { get; private set; }
        public PersonAvailabilityEvent(long personId, string personName, bool newState) : base(0, "PersonAvailability")
        {
            this.Id_Person = personId;
            this.IsAvailable = newState;
            this.PersonName = personName;
        }
    }

    /// <summary>
    ///   Event wird ausgelöst, wenn eine Person ein GeoFence betritt oder verlässt
    /// </summary>
    public class PersonGeoFenceEvent : NetworkEvent
    {
        public long Id_Person { get; private set; }
        public IGeoFence? CurrentGeoFence { get; private set; }
        public IGeoFence? PreviousGeoFence { get; private set; }
        public string PersonName { get; private set; }
        public PersonGeoFenceEvent(long personId, string personName, IGeoFence? newState, IGeoFence? previousGeoFence) : base(0, "PersonGeoFence")
        {
            this.Id_Person = personId;
            this.CurrentGeoFence = newState;
            this.PersonName = personName;
            this.PreviousGeoFence = previousGeoFence;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn sich die Luftqualität in einem Raum ändert
    /// </summary>
    public class AirQualityChangedEvent : NetworkEvent
    {
        public AirQualityChangedEvent(byte source ,
            AirQualitityLevel level, string msg, string roomName, [CallerMemberName] string? caller = null) : base(source, caller ?? "AirQualityChangedEvent")
        {
            this.Level = level;
            this.Message = msg;
            this.RoomName = roomName;
        }

        public AirQualitityLevel Level { get;  }
        public string Message { get;  }
        public string RoomName { get; }
    }

    public class SayEvent
    {
        public SayEvent(string msg, string targetSpeaker = "", SpeechVolume vol = SpeechVolume.Normal, [CallerMemberName] string? caller = null) 
        {
            this.Message = msg;
            this.TargetSpeaker = targetSpeaker;
            this.Volume = vol;
        }

        public string Message { get; }
        public string TargetSpeaker { get; }
        public SpeechVolume Volume { get; }
    }

    public class StartAudioEvent {
        public StartAudioEvent(string audioFileName, string targetSpeaker = "", [CallerMemberName] string? caller = null) 
        {
            this.TargetSpeaker = targetSpeaker;
            this.AudioFileName = audioFileName;
        }

        public string TargetSpeaker { get; }
        public string AudioFileName { get; }
    }

    public class StopAudioEvent 
    {
        public StopAudioEvent(string targetSpeaker = "", [CallerMemberName] string? caller = null) 
        {
            this.TargetSpeaker = targetSpeaker;
        }

        public string TargetSpeaker { get; }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn sich die Außentemperatur ändert
    /// </summary>
    public class OutDoorTemperatureChangedEvent : NetworkEvent
    {
        public OutDoorTemperatureChangedEvent(double newVal) : base(0, "OutDoorTemperatureChanged")
        {
            this.NewValue = newVal;
        }

        public double NewValue { get; }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn eine Wetterwarnung vorliegt
    /// </summary>
    public class WeatherWarningEvent : NetworkEvent
    {
        public WeatherWarningEvent(string newVal) : base(0, "WeatherWarning")
        {
            this.NewValue = newVal;
        }

        public string NewValue { get; }
    }
}
