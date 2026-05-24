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
    /// Event wird ausgelöst, wenn eine Steckdose ihren Ein/Aus-Zustand ändert.
    /// </summary>
    public class WallPlugEnabledChangedEvent : NetworkEvent
    {
        public bool IsOn { get; }

        public WallPlugEnabledChangedEvent(byte source, bool isOn)
            : base(source, "WallPlugEnabled")
        {
            IsOn = isOn;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn eine Steckdose in den niedrigen Leistungsbereich wechselt.
    /// </summary>
    public class WallPlugPowerLowEvent : NetworkEvent
    {
        public WallPlugPowerLowEvent(byte source)
            : base(source, "WallPlugPowerLow")
        {
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn eine Steckdose in den hohen Leistungsbereich wechselt.
    /// </summary>
    public class WallPlugPowerHighEvent : NetworkEvent
    {
        public WallPlugPowerHighEvent(byte source)
            : base(source, "WallPlugPowerHigh")
        {
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

    /// <summary>
    /// Event wird ausgelöst, wenn ein geplanter Alarm feuert.
    /// </summary>
    public class AlarmTriggeredEvent : NetworkEvent
    {
        public long AlarmScheduleId { get; }

        public int ContentType { get; }

        public string? ContentJson { get; }

        public string DisplayText { get; }

        public bool IsSuppressedBySummer { get; }

        public AlarmTriggeredEvent(
            long alarmScheduleId,
            int contentType,
            string? contentJson,
            string displayText,
            bool isSuppressedBySummer)
            : base(0, "AlarmTriggered")
        {
            AlarmScheduleId = alarmScheduleId;
            ContentType = contentType;
            ContentJson = contentJson;
            DisplayText = displayText;
            IsSuppressedBySummer = isSuppressedBySummer;
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
        public WeatherWarningEvent(
            string location,
            string outputString,
            IReadOnlyList<string> warnings,
            IReadOnlyList<WeatherWarningDetail> warningDetails) : base(0, "WeatherWarning")
        {
            Location = location;
            OutputString = outputString;
            Warnings = warnings;
            WarningDetails = warningDetails;
        }

        public string Location { get; }
        public string OutputString { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<WeatherWarningDetail> WarningDetails { get; }
    }

    /// <summary>
    /// Structured weather warning details from DWD.
    /// </summary>
    public class WeatherWarningDetail
    {
        public WeatherWarningDetail(
            string key,
            string? regionName,
            string? description,
            string? @event,
            string? headline,
            string? instruction,
            int? type,
            int? level,
            DateTime? startDate,
            DateTime? endDate,
            bool isAllDayWarning,
            string outputString)
        {
            Key = key;
            RegionName = regionName;
            Description = description;
            Event = @event;
            Headline = headline;
            Instruction = instruction;
            Type = type;
            Level = level;
            StartDate = startDate;
            EndDate = endDate;
            IsAllDayWarning = isAllDayWarning;
            OutputString = outputString;
        }

        public string Key { get; }
        public string? RegionName { get; }
        public string? Description { get; }
        public string? Event { get; }
        public string? Headline { get; }
        public string? Instruction { get; }
        public int? Type { get; }
        public int? Level { get; }
        public DateTime? StartDate { get; }
        public DateTime? EndDate { get; }
        public bool IsAllDayWarning { get; }
        public string OutputString { get; }
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
    /// Event wird ausgelöst, wenn eine Tür oder ein Fenster geöffnet wurde.
    /// </summary>
    public class DoorOrWindowOpenedEvent : NetworkEvent
    {
        public bool IsWindow { get; }
        public string DeviceName { get; }
        public string DeviceRoom { get; }
        public IReadOnlyList<string> TurnedOffHeatings { get; }

        public DoorOrWindowOpenedEvent(byte source, bool isWindow, string deviceName, string deviceRoom, IReadOnlyList<string> turnedOffHeatings)
            : base(source, "DoorOrWindowOpened")
        {
            IsWindow = isWindow;
            DeviceName = deviceName;
            DeviceRoom = deviceRoom;
            TurnedOffHeatings = turnedOffHeatings;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn eine Tür oder ein Fenster noch geöffnet ist.
    /// </summary>
    public class DoorOrWindowStillOpenEvent : NetworkEvent
    {
        public bool IsWindow { get; }
        public string DeviceName { get; }
        public string DeviceRoom { get; }
        public TimeSpan OpenedSince { get; }

        public DoorOrWindowStillOpenEvent(byte source, bool isWindow, string deviceName, string deviceRoom, TimeSpan openedSince)
            : base(source, "DoorOrWindowStillOpen")
        {
            IsWindow = isWindow;
            DeviceName = deviceName;
            DeviceRoom = deviceRoom;
            OpenedSince = openedSince;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn eine Tür oder ein Fenster geschlossen wurde.
    /// </summary>
    public class DoorOrWindowClosedEvent : NetworkEvent
    {
        public bool IsWindow { get; }
        public string DeviceName { get; }
        public string DeviceRoom { get; }
        public IReadOnlyList<string> TurnedOnHeatings { get; }
        public TimeSpan OpenedDuration { get; }

        public DoorOrWindowClosedEvent(byte source, bool isWindow, string deviceName, string deviceRoom, IReadOnlyList<string> turnedOnHeatings, TimeSpan openedDuration)
            : base(source, "DoorOrWindowClosed")
        {
            IsWindow = isWindow;
            DeviceName = deviceName;
            DeviceRoom = deviceRoom;
            TurnedOnHeatings = turnedOnHeatings;
            OpenedDuration = openedDuration;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn sich der aktuelle Stromnetzstatus ändert.
    /// </summary>
    public class GridStateChangedEvent : NetworkEvent
    {
        public GridStateChangedEvent(
            string zip,
            int currentState,
            string currentStateText,
            int? previousState,
            string? previousStateText,
            DateTime changedAtUtc)
            : base(0, "GridStateChanged")
        {
            Zip = zip;
            CurrentState = currentState;
            CurrentStateText = currentStateText;
            PreviousState = previousState;
            PreviousStateText = previousStateText;
            ChangedAtUtc = changedAtUtc;
        }

        public string Zip { get; }
        public int CurrentState { get; }
        public string CurrentStateText { get; }
        public int? PreviousState { get; }
        public string? PreviousStateText { get; }
        public DateTime ChangedAtUtc { get; }
    }
}
