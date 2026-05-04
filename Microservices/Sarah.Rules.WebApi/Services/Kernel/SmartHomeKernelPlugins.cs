using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.Rules.Services.Clients;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;
using Sarah.ServiceClients;

namespace Sarah.Rules.Services.Kernel;

public sealed class SpeechKernelPlugin
{
    private readonly RabbitMQClient _rabbitMq;
    private readonly SmartHomeKernelService.ConversationSpeechContext? _speechContext;
    private readonly ILogger<SpeechKernelPlugin> _logger;

    internal SpeechKernelPlugin(
        RabbitMQClient rabbitMq,
        SmartHomeKernelService.ConversationSpeechContext? speechContext,
        ILogger<SpeechKernelPlugin> logger)
    {
        _rabbitMq = rabbitMq;
        _speechContext = speechContext;
        _logger = logger;
    }

    [KernelFunction, Description("Gibt einen Sprachtext auf einem Speaker oder auf allen Speakern aus.")]
    public async Task<string> SayAsync(
        [Description("Der zu sprechende deutsche Text.")] string text,
        [Description("Optionaler Ziel-Speaker, leer fuer Broadcast.")] string targetSpeaker = "",
        [Description("Lautstaerke: Silent, Quiet, Normal, Loud oder VeryLoud.")] string volume = "Normal")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Keine Sprachausgabe ausgefuehrt.";
        }

        if (!Enum.TryParse<SpeechVolume>(volume, ignoreCase: true, out var parsedVolume))
        {
            parsedVolume = SpeechVolume.Normal;
        }

        string resolvedTargetSpeaker = ResolveTargetSpeaker(targetSpeaker, _speechContext?.TargetSpeaker);
        await _rabbitMq.PublishAsync(new SayMessage(text, resolvedTargetSpeaker, parsedVolume));
        _speechContext?.MarkSpeechOutput();

        _logger.LogInformation("Kernel speech output published for speaker '{Speaker}'", resolvedTargetSpeaker);
        return $"Sprachausgabe gesendet: {text}";
    }

    internal static string ResolveTargetSpeaker(string requestedTargetSpeaker, string? fallbackTargetSpeaker)
    {
        if (!string.IsNullOrWhiteSpace(requestedTargetSpeaker))
        {
            return requestedTargetSpeaker.Trim();
        }

        return string.IsNullOrWhiteSpace(fallbackTargetSpeaker)
            ? string.Empty
            : fallbackTargetSpeaker.Trim();
    }
}

public sealed class AudioKernelPlugin
{
    private readonly RabbitMQClient _rabbitMq;

    public AudioKernelPlugin(RabbitMQClient rabbitMq)
    {
        _rabbitMq = rabbitMq;
    }

    [KernelFunction, Description("Startet die Wiedergabe einer Audiodatei auf einem Speaker oder als Broadcast.")]
    public async Task<string> StartAudioAsync(string audioFileName, string targetSpeaker = "")
    {
        await _rabbitMq.PublishAsync(new StartAudioMessage(audioFileName, targetSpeaker));
        return $"Audio gestartet: {audioFileName}";
    }

    [KernelFunction, Description("Stoppt die Audio-Wiedergabe auf einem Speaker oder auf allen Speakern.")]
    public async Task<string> StopAudioAsync(string targetSpeaker = "")
    {
        await _rabbitMq.PublishAsync(new StopAudioMessage(targetSpeaker));
        return "Audio gestoppt.";
    }
}

public sealed class DeviceControlKernelPlugin
{
    private readonly DeviceServiceClient _deviceService;

    public DeviceControlKernelPlugin(DeviceServiceClient deviceService)
    {
        _deviceService = deviceService;
    }

    [KernelFunction, Description("Aktiviert eine Szene anhand ihres Namens.")]
    public async Task<string> ActivateSceneAsync(string sceneName)
    {
        await _deviceService.ActivateScene(sceneName);
        return $"Szene aktiviert: {sceneName}";
    }

    [KernelFunction, Description("Deaktiviert eine Szene anhand ihres Namens.")]
    public async Task<string> DeactivateSceneAsync(string sceneName)
    {
        await _deviceService.DeactivateScene(sceneName);
        return $"Szene deaktiviert: {sceneName}";
    }

    [KernelFunction, Description("Schaltet eine Lampe per Node-ID um.")]
    public async Task<string> ToggleLampByNodeAsync(int nodeId)
    {
        await _deviceService.ToggleLampByNodeAsync((byte)nodeId);
        return $"Lampe Node {nodeId} umgeschaltet.";
    }

    [KernelFunction, Description("Setzt eine Lampe per Node-ID auf warmweiss.")]
    public async Task<string> SetLampWarmWhiteByNodeAsync(int nodeId)
    {
        await _deviceService.SetLampWarmWhiteByNodeAsync((byte)nodeId);
        return $"Lampe Node {nodeId} auf warmweiss gesetzt.";
    }

    [KernelFunction, Description("Setzt eine Lampe per Node-ID auf kaltweiss.")]
    public async Task<string> SetLampColdWhiteByNodeAsync(int nodeId)
    {
        await _deviceService.SetLampColdWhiteByNodeAsync((byte)nodeId);
        return $"Lampe Node {nodeId} auf kaltweiss gesetzt.";
    }

    [KernelFunction, Description("Setzt eine Lampe per Node-ID auf Farbe und Helligkeit.")]
    public async Task<string> SetLampColorAndBrightnessByNodeAsync(int nodeId, string color, int brightness)
    {
        await _deviceService.SetLampColorAndBrightnessByNodeAsync((byte)nodeId, color, (byte)brightness);
        return $"Lampe Node {nodeId} auf Farbe {color} und Helligkeit {brightness} gesetzt.";
    }

    [KernelFunction, Description("Schaltet einen WallPlug per Node-ID ein oder aus.")]
    public async Task<string> SetWallPlugStateByNodeAsync(int nodeId, bool isOn)
    {
        await _deviceService.SetWallPlugStateByNodeAsync((byte)nodeId, isOn);
        return $"WallPlug Node {nodeId} auf {(isOn ? "an" : "aus")} gesetzt.";
    }

    [KernelFunction, Description("Schaltet einen WallPlug per Node-ID um.")]
    public async Task<string> ToggleWallPlugByNodeAsync(int nodeId)
    {
        await _deviceService.ToggleWallPlugByNodeAsync((byte)nodeId);
        return $"WallPlug Node {nodeId} umgeschaltet.";
    }

    [KernelFunction, Description("Setzt die Thermostat-Temperatur fuer ein Device anhand der Device-ID.")]
    public async Task<string> SetThermostatTemperatureAsync(long deviceId, double temperature)
    {
        await _deviceService.SetThermostatTemperatureAsync(deviceId, (float)temperature);
        return $"Thermostat {deviceId} auf {temperature:F1} Grad gesetzt.";
    }
}

public sealed class WeatherKernelPlugin
{
    private readonly IWeatherProvider _weatherProvider;

    public WeatherKernelPlugin(IWeatherProvider weatherProvider)
    {
        _weatherProvider = weatherProvider;
    }

    [KernelFunction, Description("Liefert die aktuellen Wetterdaten inklusive Temperatur, Wettertext und Tagesvorhersage. Außerdem der Zeitpunkt des Sonnenaufgangs und Sonnenuntergangs.")]
    public string GetCurrentWeather()
    {
        var currentWeather = _weatherProvider.GetCurrentWeatherString();
        var currentTemperature = _weatherProvider.CurrentOutdoorTemperature;
        var averageNext4Hours = _weatherProvider.AverageTemperatureNext4Hours;
        var sunrise = _weatherProvider.GetSunrise();
        var sunset = _weatherProvider.GetSunset();
        var forecastToday = _weatherProvider.GetWeatherForecastStringForToday();

        var averageText = averageNext4Hours.HasValue
            ? $"{averageNext4Hours.Value:F1}"
            : "nicht verfuegbar";
        var sunriseText = sunrise.HasValue
            ? sunrise.Value.ToString("O")
            : "nicht verfuegbar";
        var sunsetText = sunset.HasValue
            ? sunset.Value.ToString("O")
            : "nicht verfuegbar";

        return $"Temperatur aktuell: {currentTemperature:F1} Grad C. " +
               $"Durchschnitt naechste 4 Stunden: {averageText}. " +
               $"Sonnenaufgang: {sunriseText}. " +
               $"Sonnenuntergang: {sunsetText}. " +
               $"Aktuelles Wetter: {currentWeather}. " +
               $"Vorhersage heute: {forecastToday}";
    }

    [KernelFunction, Description("Liefert aktuelle Wetterwarnungen.")]
    public string GetCurrentWarnings()
    {
        var warnings = _weatherProvider.GetWeatherWarningString();
        return string.IsNullOrWhiteSpace(warnings)
            ? "Keine aktuellen Wetterwarnungen vorhanden."
            : warnings;
    }
}

public sealed class GridStateKernelPlugin
{
    private readonly IGridStateProvider _gridStateProvider;

    public GridStateKernelPlugin(IGridStateProvider gridStateProvider)
    {
        _gridStateProvider = gridStateProvider;
    }

    [KernelFunction, Description("Liefert den aktuellen Stromnetzstatus (StromGedacht Grid Stage) fuer die konfigurierte Region.")]
    public string GetCurrentGridState()
    {
        var state = _gridStateProvider.CurrentGridState;
        if (!state.HasValue)
        {
            return "Es liegen noch keine Stromnetzstatus-Daten vor.";
        }

        string zip = string.IsNullOrWhiteSpace(_gridStateProvider.CurrentZipCode)
            ? "unbekannt"
            : _gridStateProvider.CurrentZipCode;
        string changedAt = _gridStateProvider.LastChangedAtUtc?.ToString("O") ?? "unbekannt";

        return $"Aktueller Stromnetzstatus fuer PLZ {zip}: {_gridStateProvider.CurrentGridStateText} (Code {state.Value}). Letzte Aenderung: {changedAt}.";
    }
}

public sealed class GridStateForecastKernelPlugin
{
    private readonly StromGedachtGridStatesApiClient _gridStatesClient;
    private readonly IConfiguration _configuration;

    public GridStateForecastKernelPlugin(StromGedachtGridStatesApiClient gridStatesClient, IConfiguration configuration)
    {
        _gridStatesClient = gridStatesClient;
        _configuration = configuration;
    }

    [KernelFunction, Description("Liefert eine kurzfristige Empfehlung anhand der prognostizierten Stromnetzstufen (StromGedacht statesRelative).")]
    public async Task<string> GetGridStateAdvisoryAsync(int hoursInFuture = 12)
    {
        int windowHours = Math.Clamp(hoursInFuture, 1, 48);
        string zip = _configuration["StromGedacht:ZipCode"] ?? "71638";
        string? b2bId = _configuration["StromGedacht:B2BId"];

        var forecast = await _gridStatesClient.GetStatesRelativeAsync(
            zip: zip,
            hoursInFuture: windowHours,
            hoursInPast: 0,
            b2bId: b2bId);

        var states = forecast.States ?? new List<StromGedachtGridStateWindow>();
        if (states.Count == 0)
        {
            return $"Keine Stromnetzprognose fuer PLZ {zip} verfuegbar.";
        }

        var normalized = states.OrderBy(s => s.From).ToList();
        int worstState = normalized.Max(s => Priority(s.State));
        var bestGreenWindow = normalized
            .Where(s => s.State is -1 or 1)
            .OrderBy(s => s.From)
            .FirstOrDefault();

        string overall = worstState switch
        {
            >= 3 => "angespannt",
            2 => "mittel",
            _ => "entspannt"
        };

        string recommendation = worstState switch
        {
            >= 4 => "Empfehlung: vermeide flexible Grossverbraucher in diesem Zeitraum.",
            3 => "Empfehlung: verschiebe flexible Lasten wenn moeglich in gruenere Zeitfenster.",
            _ => "Empfehlung: Lastverschiebung aktuell nicht notwendig."
        };

        string greenHint = bestGreenWindow == null
            ? "Kein grueneres Zeitfenster innerhalb des Prognosehorizonts gefunden."
            : $"Naechstes gutes Zeitfenster: {bestGreenWindow.From:O} bis {bestGreenWindow.To:O} ({ToStateText(bestGreenWindow.State)}).";

        return $"Stromnetzprognose fuer PLZ {zip} (naechste {windowHours}h): insgesamt {overall}. " +
               $"{recommendation} {greenHint}";
    }

    private static int Priority(int state)
    {
        return state switch
        {
            4 => 4,
            3 => 3,
            1 => 2,
            -1 => 1,
            _ => 2
        };
    }

    private static string ToStateText(int state)
    {
        return state switch
        {
            -1 => "superGreen",
            1 => "green",
            3 => "orange",
            4 => "red",
            _ => "unknown"
        };
    }
}

public sealed class PresenceKernelPlugin
{
    private readonly IPersonService _personService;

    public PresenceKernelPlugin(IPersonService personService)
    {
        _personService = personService;
    }

    [KernelFunction, Description("Gibt eine Zusammenfassung der Anwesenheit von Personen zu Hause aus.")]
    public async Task<string> GetPresenceSummaryAsync()
    {
        var persons = (await _personService.GetAllPersonsAsync()).ToList();
        var atHome = persons.Where(p => p.IsAtHome).ToList();

        if (!persons.Any())
        {
            return "Keine Personen konfiguriert.";
        }

        if (!atHome.Any())
        {
            return $"Niemand ist zu Hause. Konfigurierte Personen: {string.Join(", ", persons.Select(p => p.Name))}.";
        }

        return $"{atHome.Count} von {persons.Count} Personen sind zu Hause: {string.Join(", ", atHome.Select(p => p.Name))}.";
    }

    [KernelFunction, Description("Prueft, ob aktuell jemand zu Hause ist.")]
    public async Task<string> IsSomeonePresentAsync()
    {
        bool present = await _personService.IsSomeonePresent();
        return present ? "Ja, mindestens eine Person ist zu Hause." : "Nein, aktuell ist niemand zu Hause.";
    }

    [KernelFunction, Description("Liefert die Namen aller Personen, die aktuell zu Hause sind.")]
    public async Task<string> GetAtHomePersonsAsync()
    {
        var atHome = (await _personService.GetAllPersonsAsync())
            .Where(p => p.IsAtHome)
            .Select(p => p.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        return atHome.Any()
            ? string.Join(", ", atHome)
            : "Keine Person ist aktuell zu Hause.";
    }
}

public sealed class DeviceQueryKernelPlugin
{
    private readonly DeviceServiceClient _deviceService;

    public DeviceQueryKernelPlugin(DeviceServiceClient deviceService)
    {
        _deviceService = deviceService;
    }

    [KernelFunction, Description("Liefert eine Uebersicht der aktuellen Geraetelandschaft inkl. Anzahl nach Geraetetyp.")]
    public async Task<string> GetDevicesSummaryAsync()
    {
        var devices = await _deviceService.GetAllDevicesAsync();
        if (devices.Count == 0)
        {
            return "Keine Geraete gefunden.";
        }

        var grouped = devices
            .GroupBy(d => string.IsNullOrWhiteSpace(d.TypeName) ? d.DeviceType.ToString() : d.TypeName)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        return $"Gesamtgeraete: {devices.Count}. Typen: {string.Join("; ", grouped)}.";
    }

    [KernelFunction, Description("Liefert Details zu einem Geraet anhand der Node-ID.")]
    public async Task<string> GetDeviceByNodeIdAsync(int nodeId)
    {
        if (nodeId < byte.MinValue || nodeId > byte.MaxValue)
        {
            return $"Ungueltige Node-ID: {nodeId}.";
        }

        var device = await _deviceService.GetDeviceByNodeIdAsync((byte)nodeId);
        return device == null
            ? $"Kein Geraet fuer Node {nodeId} gefunden."
            : FormatDevice(device);
    }

    [KernelFunction, Description("Liefert Details zu einem Geraet anhand der Device-ID.")]
    public async Task<string> GetDeviceByIdAsync(long deviceId)
    {
        var device = await _deviceService.GetDeviceByIdAsync(deviceId);
        return device == null
            ? $"Kein Geraet mit ID {deviceId} gefunden."
            : FormatDevice(device);
    }

    [KernelFunction, Description("Liefert eine Liste aktuell offener Tueren.")]
    public async Task<string> GetOpenDoorsAsync()
    {
        var openDoors = await _deviceService.GetOpenDoors();
        if (string.IsNullOrWhiteSpace(openDoors.OpenDoorInfo))
        {
            return "Aktuell sind keine Tueren offen.";
        }

        return "Offene Tueren: " + openDoors.OpenDoorInfo;
    }

    private static string FormatDevice(Sarah.API.BusinessObjects.DTOs.DeviceDto device)
    {
        var name = string.IsNullOrWhiteSpace(device.Name) ? "(ohne Name)" : device.Name;
        string state;

        if (device.Lamp != null)
        {
            state = $"Lampe Helligkeit {device.Lamp.Brightness}, Farbe {device.Lamp.Color ?? "unbekannt"}";
        }
        else if (device.WallPlug != null)
        {
            state = $"WallPlug {(device.WallPlug.IsOn ? "an" : "aus")}";
        }
        else if (device.Thermostat != null)
        {
            state = $"Thermostat Solltemperatur {device.Thermostat.TemperatureSetpoint?.ToString("F1") ?? "unbekannt"}";
        }
        else if (device.DoorSensor != null)
        {
            state = $"DoorSensor Zustand {device.DoorSensor.State}";
        }
        else
        {
            state = "Kein spezifischer Zustandsblock verfuegbar";
        }

        return $"Device {device.Id}, Node {device.NodeId}, Name {name}, Typ {device.DeviceType}: {state}.";
    }
}

public sealed class RoomStateKernelPlugin
{
    private readonly RoomServiceClient _roomService;
    private readonly DeviceServiceClient _deviceService;

    public RoomStateKernelPlugin(RoomServiceClient roomService, DeviceServiceClient deviceService)
    {
        _roomService = roomService;
        _deviceService = deviceService;
    }

    [KernelFunction, Description("Liefert eine Liste aller Raeume.")]
    public async Task<string> GetAllRoomsAsync()
    {
        var rooms = (await _roomService.GetAllRoomsAsync())
            .OrderBy(r => r.Name)
            .ToList();

        if (rooms.Count == 0)
        {
            return "Keine Raeume gefunden.";
        }

        string roomList = string.Join(", ", rooms.Select(r => $"{r.Id}: {r.Name}"));
        return $"Raeume ({rooms.Count}): {roomList}.";
    }

    [KernelFunction, Description("Liefert alle Geraete in einem Raum anhand der Room-ID.")]
    public async Task<string> GetDevicesInRoomAsync(long roomId)
    {
        var rooms = await _roomService.GetAllRoomsAsync();
        var room = rooms.FirstOrDefault(r => r.Id == roomId);
        if (room == null)
        {
            return $"Kein Raum mit ID {roomId} gefunden.";
        }

        var devices = (await _deviceService.GetAllDevicesAsync())
            .Where(d => d.RoomId == roomId)
            .OrderBy(d => d.Name)
            .ThenBy(d => d.NodeId)
            .ToList();

        if (devices.Count == 0)
        {
            return $"Im Raum {room.Name} (ID {roomId}) sind keine Geraete hinterlegt.";
        }

        string list = string.Join("; ", devices.Select(d => FormatDeviceShort(d)));
        return $"Geraete im Raum {room.Name} (ID {roomId}), Anzahl {devices.Count}: {list}.";
    }

    [KernelFunction, Description("Liefert den aktuellen Zustand eines Raums: Durchschnittstemperatur, offene Tueren und Praesenz.")]
    public async Task<string> GetRoomStateAsync(long roomId)
    {
        var rooms = await _roomService.GetAllRoomsAsync();
        var room = rooms.FirstOrDefault(r => r.Id == roomId);
        if (room == null)
        {
            return $"Kein Raum mit ID {roomId} gefunden.";
        }

        var summary = await _deviceService.GetRoomSummaryAsync(roomId);
        if (summary == null)
        {
            return $"Kein Raumzustand fuer {room.Name} (ID {roomId}) verfuegbar.";
        }

        var devices = (await _deviceService.GetAllDevicesAsync())
            .Where(d => d.RoomId == roomId)
            .ToList();

        var openDoorDevices = devices
            .Where(d => d.DoorSensor?.State == Sarah.API.BusinessObjects.DoorSensorState.Offen)
            .Select(d => string.IsNullOrWhiteSpace(d.Name) ? $"Node {d.NodeId}" : d.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        string averageTemperature = summary.AverageTemperature.HasValue
            ? $"{summary.AverageTemperature.Value:F1} Grad"
            : "nicht verfuegbar";
        string presence = summary.AnyPresence ? "ja" : "nein";
        string openDoors = openDoorDevices.Count == 0
            ? "keine"
            : string.Join(", ", openDoorDevices);

        return $"Raumzustand {room.Name} (ID {roomId}): Temperatur {averageTemperature}, Praesenz {presence}, offene Tueren/Fenster {openDoors}.";
    }

    private static string FormatDeviceShort(Sarah.API.BusinessObjects.DTOs.DeviceDto device)
    {
        string name = string.IsNullOrWhiteSpace(device.Name) ? "(ohne Name)" : device.Name;
        string type = string.IsNullOrWhiteSpace(device.TypeName) ? device.DeviceType.ToString() : device.TypeName;
        return $"{name} [Node {device.NodeId}, Typ {type}]";
    }
}

public sealed class TimeContextKernelPlugin
{
    private readonly IConfiguration _configuration;

    public TimeContextKernelPlugin(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [KernelFunction, Description("Liefert aktuellen Zeitkontext mit Datum, Uhrzeit, Wochentag, Wochenende und Ruhezeit.")]
    public string GetCurrentTimeContext()
    {
        var now = DateTime.Now;
        var (startHour, endHour) = GetQuietHours();
        bool weekend = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        bool quietNow = IsInQuietHours(now, startHour, endHour);

        return $"Lokale Zeit: {now:O}. Wochentag: {now:dddd}. Wochenende: {(weekend ? "ja" : "nein")}. " +
               $"Ruhezeit aktiv: {(quietNow ? "ja" : "nein")}. Ruhezeit-Konfiguration: {startHour:00}:00 bis {endHour:00}:00.";
    }

    [KernelFunction, Description("Prueft, ob aktuell Ruhezeit aktiv ist.")]
    public string IsQuietHoursNow()
    {
        var now = DateTime.Now;
        var (startHour, endHour) = GetQuietHours();
        bool quietNow = IsInQuietHours(now, startHour, endHour);
        return quietNow ? "Ja, aktuell ist Ruhezeit." : "Nein, aktuell ist keine Ruhezeit.";
    }

    [KernelFunction, Description("Liefert den naechsten Beginn und das naechste Ende der Ruhezeit.")]
    public string GetNextQuietHoursWindow()
    {
        var now = DateTime.Now;
        var (startHour, endHour) = GetQuietHours();

        DateTime start = new DateTime(now.Year, now.Month, now.Day, startHour, 0, 0);
        DateTime end = new DateTime(now.Year, now.Month, now.Day, endHour, 0, 0);

        if (startHour > endHour)
        {
            end = end.AddDays(1);
        }

        if (now > end)
        {
            start = start.AddDays(1);
            end = end.AddDays(1);
        }
        else if (now > start && now <= end)
        {
            start = start.AddDays(1);
            end = end.AddDays(1);
        }

        return $"Naechste Ruhezeit: Start {start:O}, Ende {end:O}.";
    }

    private (int startHour, int endHour) GetQuietHours()
    {
        int startHour = int.TryParse(_configuration["SilentStartHour"], out int start) ? start : 22;
        int endHour = int.TryParse(_configuration["SilentEndHour"], out int end) ? end : 6;
        return (startHour, endHour);
    }

    private static bool IsInQuietHours(DateTime now, int startHour, int endHour)
    {
        if (startHour == 0 && endHour == 0)
        {
            return false;
        }

        var start = new DateTime(now.Year, now.Month, now.Day, startHour, 0, 0);
        var end = new DateTime(now.Year, now.Month, now.Day, endHour, 0, 0);

        if (startHour > endHour)
        {
            end = end.AddDays(1);
            if (now < start)
            {
                start = start.AddDays(-1);
            }
        }

        return now >= start && now <= end;
    }
}

public sealed class RulesMemoryKernelPlugin
{
    private const string ConversationId = "smart-home-main";

    private readonly ApplicationDbContext _db;

    public RulesMemoryKernelPlugin(ApplicationDbContext db)
    {
        _db = db;
    }

    [KernelFunction, Description("Liefert die letzten Konversationseintraege aus dem Rule-Memory.")]
    public async Task<string> GetRecentConversationAsync(int limit = 10)
    {
        int safeLimit = Math.Clamp(limit, 1, 50);
        var rows = await _db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(safeLimit)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return "Keine Konversationseintraege vorhanden.";
        }

        rows.Reverse();
        return string.Join(Environment.NewLine,
            rows.Select(r => $"[{r.CreatedAtUtc:O}] {r.Role}: {r.Content}"));
    }

    [KernelFunction, Description("Sucht in den letzten Konversationseintraegen nach einem Stichwort.")]
    public async Task<string> SearchConversationAsync(string keyword, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return "Bitte ein Suchstichwort angeben.";
        }

        int safeLimit = Math.Clamp(limit, 1, 50);
        string needle = keyword.Trim().ToLowerInvariant();

        var rows = await _db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(200)
            .ToListAsync();

        var matches = rows
            .Where(r => r.Content.ToLower().Contains(needle))
            .Take(safeLimit)
            .ToList();

        if (matches.Count == 0)
        {
            return $"Keine Treffer fuer '{keyword}' gefunden.";
        }

        matches.Reverse();
        return string.Join(Environment.NewLine,
            matches.Select(r => $"[{r.CreatedAtUtc:O}] {r.Role}: {r.Content}"));
    }

    [KernelFunction, Description("Speichert eine kurze Notiz im Rule-Memory fuer spaetere Entscheidungen.")]
    public async Task<string> RememberAsync(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return "Leere Notiz wurde nicht gespeichert.";
        }

        _db.KernelConversationMessages.Add(new KernelConversationMessageEntity
        {
            ConversationId = ConversationId,
            Role = "tool",
            Content = "memory-note: " + note.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return "Notiz gespeichert.";
    }

    [KernelFunction, Description("Prueft, ob ein Text in den letzten Minuten bereits in der Konversation vorkam.")]
    public async Task<string> WasMentionedRecentlyAsync(string text, int withinMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Bitte einen Text zum Pruefen angeben.";
        }

        int safeMinutes = Math.Clamp(withinMinutes, 1, 1440);
        var cutoff = DateTime.UtcNow.AddMinutes(-safeMinutes);
        string needle = text.Trim().ToLowerInvariant();

        bool exists = await _db.KernelConversationMessages
            .AnyAsync(m => m.ConversationId == ConversationId
                           && m.CreatedAtUtc >= cutoff
                           && m.Content.ToLower().Contains(needle));

        return exists
            ? $"Ja, der Text wurde in den letzten {safeMinutes} Minuten bereits erwaehnt."
            : $"Nein, der Text wurde in den letzten {safeMinutes} Minuten nicht erwaehnt.";
    }
}