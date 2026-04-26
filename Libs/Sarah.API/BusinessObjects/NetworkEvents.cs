using Sarah.API.Interfaces;
using System;
using System.Collections.Generic;
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
        public string? CurrentGeoFence { get; private set; }
        public string? PreviousGeoFence { get; private set; }
        public string PersonName { get; private set; }
        public PersonGeoFenceEvent(long personId, string personName, string? newState, string? previousGeoFence) : base(0, "PersonGeoFence")
        {
            this.Id_Person = personId;
            this.PersonName = personName;
            this.CurrentGeoFence = newState;
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

    /// <summary>
    /// Event wird ausgelöst, wenn ein Türsensor seinen Öffnungsstatus ändert
    /// </summary>
    public class DoorSensorStateChangedEvent : NetworkEvent
    {
        public bool IsOpen { get; }
        public DoorSensorStateChangedEvent(byte source, bool isOpen) : base(source, "DoorState")
        {
            this.IsOpen = isOpen;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn der Knopf eines GPS-Trackers gedrückt oder losgelassen wird
    /// </summary>
    public class TrackerButtonPressedEvent : NetworkEvent
    {
        public bool IsPressed { get; }
        public TrackerButtonPressedEvent(byte source, bool isPressed) : base(source, "TrackerButton")
        {
            this.IsPressed = isPressed;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn eine Steckdose ihren Zustand oder Leistungsschwelle ändert
    /// </summary>
    public class WallPlugStateChangedEvent : NetworkEvent
    {
        public bool IsOn { get; }
        public DateTime LastChangeToPowerLow { get; }
        public DateTime LastIncreasePower { get; }
        public DateTime LastDecreasePower { get; }
        public WallPlugStateChangedEvent(byte source, bool isOn, DateTime lastChangeToPowerLow, DateTime lastIncreasePower, DateTime lastDecreasePower)
            : base(source, "WallPlugState")
        {
            IsOn = isOn;
            LastChangeToPowerLow = lastChangeToPowerLow;
            LastIncreasePower = lastIncreasePower;
            LastDecreasePower = lastDecreasePower;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn ein MultiSensor Präsenz oder Helligkeit ändert
    /// </summary>
    public class MultiSensorStateChangedEvent : NetworkEvent
    {
        public float? Presence { get; }
        public float? Luminance { get; }
        public MultiSensorStateChangedEvent(byte source, float? presence, float? luminance)
            : base(source, "MultiSensorState")
        {
            Presence = presence;
            Luminance = luminance;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn ein Rauchmelder Alarm auslöst oder zurückgesetzt wird
    /// </summary>
    public class SmokeSensorAlertEvent : NetworkEvent
    {
        public bool AlarmActive { get; }
        public SmokeSensorAlertEvent(byte source, bool alarmActive) : base(source, "SmokeSensorAlert")
        {
            AlarmActive = alarmActive;
        }
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

    /// <summary>
    /// Event wird ausgelöst, wenn eine Wettervorhersage aktualisiert wurde
    /// </summary>
    public class WeatherForecastUpdatedEvent : NetworkEvent
    {
        public WeatherForecastUpdatedEvent(string forecastStringForToday) : base(0, "WeatherForecastUpdated")
        {
            this.ForecastStringForToday = forecastStringForToday;
        }

        public string ForecastStringForToday { get; }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn der BatteryMonitor kritisch niedrige Batteriezustände meldet.
    /// </summary>
    public class BatteryWarningEvent : NetworkEvent
    {
        public BatteryWarningEvent(IReadOnlyList<BatteryDeviceInfo> warnings) : base(0, "BatteryWarning")
        {
            Warnings = warnings;
        }

        /// <summary>
        /// Geräte mit kritischem Ladestand.
        /// </summary>
        public IReadOnlyList<BatteryDeviceInfo> Warnings { get; }
    }

    /// <summary>
    /// Beschreibt ein Gerät mit kritischem Batteriezustand.
    /// </summary>
    public class BatteryDeviceInfo
    {
        public BatteryDeviceInfo(string deviceName, float batteryLevel)
        {
            DeviceName = deviceName;
            BatteryLevel = batteryLevel;
        }

        public string DeviceName { get; }
        public float BatteryLevel { get; }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn der DoorMonitor eine Meldung für eine Tür/ein Fenster erzeugt.
    /// </summary>
    public class DoorMonitorAlertEvent : NetworkEvent
    {
        public DoorMonitorAlertEvent(
            byte sourceNodeId,
            string deviceName,
            bool isWindow,
            DoorMonitorAlertType alertType,
            int openDurationMinutes,
            bool wasOpenLongEnough,
            float? roomTemperature,
            IReadOnlyList<string>? heatingsTurnedOff,
            IReadOnlyList<DoorMonitorHeatingChange>? heatingChanges,
            int? nextAlertIntervalMinutes,
            bool isLoud)
            : base(sourceNodeId, "DoorMonitorAlert")
        {
            DeviceName = deviceName;
            IsWindow = isWindow;
            AlertType = alertType;
            OpenDurationMinutes = openDurationMinutes;
            WasOpenLongEnough = wasOpenLongEnough;
            RoomTemperature = roomTemperature;
            HeatingsTurnedOff = heatingsTurnedOff;
            HeatingChanges = heatingChanges;
            NextAlertIntervalMinutes = nextAlertIntervalMinutes;
            IsLoud = isLoud;
        }

        public string DeviceName { get; }
        public bool IsWindow { get; }
        public DoorMonitorAlertType AlertType { get; }
        public int OpenDurationMinutes { get; }
        public bool WasOpenLongEnough { get; }
        public float? RoomTemperature { get; }
        public IReadOnlyList<string>? HeatingsTurnedOff { get; }
        public IReadOnlyList<DoorMonitorHeatingChange>? HeatingChanges { get; }
        public int? NextAlertIntervalMinutes { get; }
        /// <summary>
        /// True when a louder volume (VeryLoud) should be used for the speech output.
        /// </summary>
        public bool IsLoud { get; }
    }

    /// <summary>
    /// Heizungsänderung die beim Schließen eines Fensters vorgenommen wurde.
    /// </summary>
    public class DoorMonitorHeatingChange
    {
        public DoorMonitorHeatingChange(string roomName, float? restoredTemperature)
        {
            RoomName = roomName;
            RestoredTemperature = restoredTemperature;
        }

        public string RoomName { get; }
        /// <summary>
        /// Wiederhergestellte Zieltemperatur nach Schließen des Fensters.
        /// Null, wenn die Heizung wegen eines anderen offenen Fensters nicht wiederhergestellt wurde.
        /// </summary>
        public float? RestoredTemperature { get; }
    }

    /// <summary>
    /// Typ der DoorMonitor-Meldung.
    /// </summary>
    public enum DoorMonitorAlertType
    {
        Opened,
        StillOpen,
        Closed
    }
}
