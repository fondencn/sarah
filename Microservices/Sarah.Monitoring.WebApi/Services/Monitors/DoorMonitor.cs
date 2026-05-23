using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.ServiceClients;
using SpeechVolume = Sarah.Messaging.RabbitMQ.Messages.SpeechVolume;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Steuerungs- und Überwachungsfunktionen für geöffnete Türen und Fenster.
    /// Hier sind alle NodeIds für Christians Wohnung fest verdrahtet!
    /// </summary>
    public class DoorMonitor(IReadOnlyList<RoomDto> _roomSnapshot, IWeatherProvider _weather, RabbitMQClient _rabbitMQ, ILogger<DoorMonitor> _logger, DeviceServiceClient _deviceServiceClient) : ICanSelfTest, IMonitor, IDoorMonitor
    {
        /// <summary>
        /// Konfiguration für jeden Fenstersensor, ab wann eine Warnung ausgegeben werden soll,
        /// um sehr kurzes öffnen zu unterdrücken und nicht zu nervige Sprachausgaben zu machen
        /// </summary>
        private Dictionary<byte, TimeSpan> DoorOpenTimeThresholds { get; } = new Dictionary<byte, TimeSpan>()
        {
            { 2,  TimeSpan.FromMinutes(5)  },  // Terassentür Arbeitszimmer
            { 23, TimeSpan.FromMinutes(1)  },  // Fenster Lukaszimmer
            { 22, TimeSpan.FromMinutes(1)  },  // Fenster Wohnzimmer
            { 44, TimeSpan.FromMinutes(10) },  // Fenster Schlafzimmer
            { 33, TimeSpan.FromMinutes(1)  },  // Balkontür Wohnzimmer
            { 34, TimeSpan.FromMinutes(1)  },  // Haustür Flur
            { 35, TimeSpan.FromMinutes(5)  },  // Terassentür Mamazimmer
        };

        /// <summary>
        /// Standart-Warnzeit, falls keine abweichende Konfiguration in <c>DoorOpenTimeThresholds</c>
        /// gemacht wurde
        /// </summary>
        private static TimeSpan DefaultOpenTimeThreshold { get; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Konfiguration, welche Heizung von welchem Fenster an/aus geschaltet werden soll
        /// </summary>
        private Dictionary<byte, byte[]> DoorToHeatingsMapping { get; } = new Dictionary<byte, byte[]>()
        {
            { 2,  new byte[]{ 253    } },  // Terassentür Arbeitszimmer macht Heizung im Wohnzimmer aus
            { 23, new byte[]{ 37    } },  // Fenster Lukaszimmer macht Heizung im Lukaszimmer aus
            { 22, new byte[]{ 253    } },  // Fenster Wohnzimmer macht Heizung im Wohnzimmer aus
            { 44, new byte[]{ 42,19 } },  // Fenster Schlafzimmer macht Heizung im Schlafzimmer und im Bad aus
            { 33, new byte[]{ 253    } },  // Balkontür Wohnzimmer macht Heizung im Wohnzimmer aus
            { 35, new byte[]{ 15    } },  // Terassentür Mamazimmer macht Heizung im Mamazimmer aus
        };

        /// <summary>
        /// Für diese Sensoren wird sofort eine Warnung ausgegben,  wenn sie geöffnet werden
        /// /// </summary>
        public byte[] WarnImmediateNodeIds { get; } = new byte[] { 22, 23, 29, 33, 34 };


        private List<SurveillanceTask> CurrentOpenDoorTasks { get; } = new List<SurveillanceTask>();


        private bool IsRunning { get; set; }

        private DateTime LastUpdate { get; set; }


        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~DoorMonitor()
        {
            if (this.CurrentOpenDoorTasks.Any())
            {
                this.CurrentOpenDoorTasks.ForEach(task => task.Cancel());
            }
        }



        /// <summary>
        /// Startet alle Überwachungsfunktionen für Türsensoren
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            if (this.IsRunning)
            {
                return;
            }

            await _rabbitMQ.SubscribeAsync<NetworkEventMessage<object>>(
                topic: MessageTopics.NetworkEvents,
                onMessage: message =>
                {
                    if (!string.Equals(message.Property, "DoorState", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.CompletedTask;
                    }

                    return Update(message.SourceNodeId);
                });

            _logger.LogDebug("DoorMonitor gestartet.");
            this.IsRunning = true;
        }

        /// <summary>
        /// Wird aufgerufen, wenn eine Device-Nachricht veröffentlicht wird
        /// </summary>
        /// <param name="changedNodeId">ID des geändertes Knotens</param>
        private async Task Update(byte changedNodeId)
        {
            try
            {

                DeviceDto? liveDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync(changedNodeId);
                DoorSensorStateDto? sensorState = liveDevice?.DoorSensor;

                if (liveDevice != null && sensorState != null)
                {
                    _logger.LogDebug("DoorMonitor: Netzwerkereignis empfangen für NodeId {NodeId}", changedNodeId);
                    byte nodeId = changedNodeId;
                    TimeSpan sensorThreshold;
                    if (!DoorOpenTimeThresholds.TryGetValue(nodeId, out sensorThreshold))
                    {
                        sensorThreshold = DefaultOpenTimeThreshold;
                    }

                    if (sensorState.State == DoorSensorState.Offen)
                    {
                        /* Tür geöffnet -> Uberwachung starten */
                        if (!this.CurrentOpenDoorTasks.Any(task => task.Device.NodeId == changedNodeId)
                            && sensorState.LastStateChanged.HasValue)
                        {
                            RoomDto? room = liveDevice.RoomId.HasValue
                                ? _roomSnapshot.FirstOrDefault(r => r.Id == liveDevice.RoomId.Value)
                                : null;

                            bool warnAtOpen = this.WarnImmediateNodeIds.Contains(nodeId);

                            byte[]? associatedHeatings;
                            if (!DoorToHeatingsMapping.TryGetValue(nodeId, out associatedHeatings))
                            {
                                associatedHeatings = null; // keine Heizung zu diesem Fenster konfiguriert...
                            }

                            this.CurrentOpenDoorTasks.Add(new SurveillanceTask(liveDevice, sensorThreshold, room, warnAtOpen, associatedHeatings, _rabbitMQ, _logger, _deviceServiceClient, sensorState.LastStateChanged.Value, _roomSnapshot));
                        }
                    }
                    else
                    {
                        /* Tür geschlossen, Überwachung beenden, kurze Sprachausgabe zur Info erzeugen */
                        SurveillanceTask? task = this.CurrentOpenDoorTasks.FirstOrDefault(task => task.Device.NodeId == changedNodeId);
                        if (task != null)
                        {
                            task.Cancel();
                            this.CurrentOpenDoorTasks.Remove(task);
                        }
                    }
                } // else: Kein DoorSensor - nichts für diese Klasse zu tun
                this.LastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren der Türzustände");
            }
        }

        /// <summary>
        /// Selbsttest ausführen
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (!this.IsRunning)
            {
                yield return new SelfTestResult(true, "Türen und Fensterüberwachung", "Nicht gestartet");
            }
            if ((DateTime.Now - this.LastUpdate) > TimeSpan.FromDays(1))
            {
                yield return new SelfTestResult(true, "Türen und Fensterüberwachung", "Letzter Sensorwert vor mehr als 1 Tag verarbeitet.");
            }
        }


        /// <summary>
        /// Ermittelt, ob momentan eine Überwachung für ein offenes Fenster läuft, dass imt der Heizung
        /// mit der angegebenen NodeID gekoppelt ist
        /// </summary>
        /// <param name="nodeID">Heizungs-ID</param>
        /// <returns></returns>
        public bool IsWindowForHeatingTracking(byte nodeID) =>
            this.CurrentOpenDoorTasks
                .SelectMany(item => item.AssociatedHeatings ?? new byte[0])
                .Any(item => item == nodeID);


        /// <summary>
        /// Gibt alle Überwachungstasks zurück, die mit der angegebenen Heizung gekoppelt sind
        /// </summary>
        /// <param name="heatingId">ID einer Heizung, die zu einem aktuell geöffneten Fenster gehört</param>
        /// <returns>Liste mit internen Überwachungstasks (daher ist die Methode private)</returns>
        private IList<SurveillanceTask> GetWindowTrackingsForHeating(byte heatingId) =>
            this.CurrentOpenDoorTasks
                .Where(item => item.AssociatedHeatings?.Contains(heatingId) == true)
                .ToList();


        /// <summary>
        /// Aktualisiert den Ziel-Wert (Schließtemperatur) für alle Überwachungsprozesse der angegebenen Heizung
        /// </summary>
        /// <param name="nodeID">Heizungs-ID</param>
        /// <param name="temperatureSetpoint">neuer Temperatur-Zielwert nach dem Schließen des Fensters</param>
        public void UpdateTargetTemperature(byte nodeID, byte temperatureSetpoint) =>
            this.CurrentOpenDoorTasks
                .Where(item => item.AssociatedHeatings?.Contains(nodeID) == true)
                .ToList()
                .ForEach(item => item.UpdateOriginalHeatingTemperature(nodeID, temperatureSetpoint));


        private class SurveillanceTask
        {
            private const float DEFAULT_OFF_TEMPERATURE = 12.0f;
            private readonly RabbitMQClient _rabbitMQ;
            private readonly ILogger<DoorMonitor> _logger;
            private readonly DeviceServiceClient _deviceServiceClient;
            private readonly DateTime _openedAt;

            /// <summary>
            /// Der Türsensor
            /// </summary>
            public DeviceDto Device { get; private set; }

            /// <summary>
            /// zugeordneter Raum
            /// </summary>
            public RoomDto? Room { get; private set; }

            /// <summary>
            /// start-Zeit, ab wann gewarnt werden soll
            /// </summary>
            public TimeSpan SensorThreshold { get; private set; }

            /// <summary>
            /// gibt an, ob sofort nach dem öffnen eine Warnung erfolgen soll (z.B. Kinderzimmer)
            /// </summary>
            public bool WarnAtOpen { get; private set; }

            /// <summary>
            /// Zugeordnete Heizkörper, die an/aus geschaltet werden sollen
            /// </summary>
            public byte[]? AssociatedHeatings { get; private set; }

            private readonly IReadOnlyList<RoomDto> _roomSnapshot;

            /// <summary>
            /// CancellationToken um den Monitoring-Task abzubrechen
            /// </summary>
            private CancellationTokenSource UpdateCancellationTokenSource { get; set; }

            /// <summary>
            /// Der laufenende Monitoring-Task
            /// </summary>
            private Task Task { get; set; }

            /// <summary>
            /// Liste mit Temperaturen, die an den zugeordneten Heizungen vor dem öffnen des Fensters
            /// ursprünglich eingestellt waren und die wieder geladen werden, sobald das Fenster geschlossen wird
            /// </summary>
            private List<KeyValuePair<byte, float>> OriginalHeatingTemperatures { get; } = new List<KeyValuePair<byte, float>>();

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="device">Der Türsensor</param>
            /// <param name="sensorThreshold">start-Zeit, ab wann gewarnt werden soll</param>
            /// <param name="room">zugeordneter Raum</param>
            /// <param name="db">Datenbankkontext</param>
            /// <param name="warnAtOpen">gibt an, ob sofort nach dem öffnen eine Warnung erfolgen soll (z.B. Kinderzimmer)</param>
            /// <param name="associatedHeatings">Zugeordnete Heizkörper, die an/aus geschaltet werden sollen</param>
            /// <param name="roomSnapshot">Snapshot der Räume</param>
            public SurveillanceTask(DeviceDto device, TimeSpan sensorThreshold, RoomDto? room, bool warnAtOpen, byte[]? associatedHeatings, RabbitMQClient rabbitMQ, ILogger<DoorMonitor> logger, DeviceServiceClient deviceServiceClient, DateTime openedAt, IReadOnlyList<RoomDto> roomSnapshot)
            {
                this._rabbitMQ = rabbitMQ;
                this._logger = logger;
                this._deviceServiceClient = deviceServiceClient;
                this._openedAt = openedAt;
                this.Device = device;
                this.Room = room;
                this.SensorThreshold = sensorThreshold;
                this.WarnAtOpen = warnAtOpen;
                this.AssociatedHeatings = associatedHeatings;
                this._roomSnapshot = roomSnapshot;

                CancellationTokenSource cts = new CancellationTokenSource();
                this.UpdateCancellationTokenSource = cts;


                /* Monitoring-Task starten und als Background Thread laufen lassen */
                this.Task = Task.Run(Tick, cts.Token);
            }

            /// <summary>
            /// dtor
            /// </summary>
            ~SurveillanceTask()
            {
                this.UpdateCancellationTokenSource?.Cancel();
                this.UpdateCancellationTokenSource?.Dispose();
            }

            /// <summary>
            /// Beendet diese Sensorüberwachung
            /// </summary>
            public void Cancel()
            {
                _logger.LogDebug("Beende überwachung der Tür/Fenster: {DeviceName}...", this.Device.Name);
                this.UpdateCancellationTokenSource.Cancel();
            }

            /// <summary>
            /// Monitoring-Loop (wird asynch ausgeführt, solange das  <c>UpdateCancellationTokenSource</c>
            /// nicht gefeuert wird
            /// </summary>
            private async void Tick()
            {
                _logger.LogDebug("Starte überwachung der geöffneten Tür/Fenster: {DeviceName}...", this.Device.Name);
                bool isWindow = this.Device.Name!.IndexOf("Fenster", StringComparison.OrdinalIgnoreCase) >= 0;
                CancellationToken cancellationToken = this.UpdateCancellationTokenSource.Token;

                try // Fängt TaskCanceledException, die beim Abbrechen des Tasks durch Cancel() erwartet wird, damit die Methode sauber mit dem Schließen des Fensters endet und die abschließende Statusmeldung veröffentlicht werden kann
                {
                    if (!this.WarnAtOpen) // Nur Manche Türen wie z.B. die Haustür lösen sofort eine Nachricht aus
                    {
                        await Task.Delay(this.SensorThreshold, cancellationToken);
                    }

                    /* Turn off all associated heatings and save their orig temp */
                    var turnedOffHeatings = await TurnOffAssociatedHeatings(cancellationToken);

                    /* Now report Open Window/Door Alert */
                    var openedMessage = new DoorOrWindowOpenedMessage()
                    {
                        IsDoor = !isWindow,
                        IsWindow = isWindow,
                        IsOpened = true,
                        TurnedOffHeatings = turnedOffHeatings,
                        DeviceName = this.Device.Name,
                        DeviceRoom = this.Room?.Name ?? ""
                    };
                    await this._rabbitMQ.PublishAsync(openedMessage, null, cancellationToken);

                    /* Now continue to report "still open" messages at increasing intervals */
                    while (!UpdateCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        TimeSpan openTime = DateTime.Now - _openedAt;
                        await Task.Delay(GetWaitTime(openTime, SensorThreshold), cancellationToken);

                        var stillOpenMessage = new DoorOrWindowStillOpenMessage()
                        {
                            IsDoor = !isWindow,
                            IsWindow = isWindow,
                            IsOpened = true,
                            OpenedSince = openTime,
                            DeviceName = this.Device.Name,
                            DeviceRoom = this.Room?.Name ?? ""
                        };
                        await this._rabbitMQ.PublishAsync(stillOpenMessage);
                    }
                }
                catch (TaskCanceledException) {/* expected when cancellation is requested - do nothing */}

                /* Tür wurde geschlossen -> ggf. Heizungen wieder anschalten */
                var turnedOnHeatings = await TurnOnAssociatedHeatings();

                /* Tür ist zu, Heizungen geändert -> Status-Message veröffentlichen, damit Rules reagieren können */
                var closedMessage = new DoorOrWindowClosedMessage()
                {
                    IsDoor = !isWindow,
                    IsWindow = isWindow,
                    IsOpened = false,
                    TurnedOnHeatings = turnedOnHeatings,
                    DeviceName = this.Device.Name,
                    DeviceRoom = this.Room?.Name ?? ""
                };
                await this._rabbitMQ.PublishAsync(closedMessage);
            }

            private async Task<TurnedOnHeatingInfo[]> TurnOnAssociatedHeatings(CancellationToken cancellationToken = default)
            {
                List<TurnedOnHeatingInfo> heatingsTurnedOn = new List<TurnedOnHeatingInfo>();
                foreach (var entry in this.OriginalHeatingTemperatures)
                {
                    byte heatingId = entry.Key;
                    float targetTemp = entry.Value;

                    try
                    {
                        DeviceDto? heatingDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync(heatingId);
                        if (heatingDevice?.Thermostat != null)
                        {
                            await _deviceServiceClient.SetThermostatTemperatureAsync(heatingDevice.Id, targetTemp);

                            heatingsTurnedOn.Add(new TurnedOnHeatingInfo
                            {
                                HeatingNodeId = heatingId,
                                HeatingName = heatingDevice.Name!,
                                HeatingRoom = heatingDevice.RoomId.HasValue
                                    ? _roomSnapshot.FirstOrDefault(r => r.Id == heatingDevice.RoomId.Value)?.Name ?? ""
                                    : ""
                            });
                        }
                        else
                        {
                            _logger.LogWarning("Heizkörper {HeatingId} nicht gefunden oder kein Thermostat", heatingId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Einschalten der Heizung {HeatingId}", heatingId);
                    }
                }

                return heatingsTurnedOn.ToArray();
            }

            private TimeSpan GetWaitTime(TimeSpan openTime, TimeSpan sensorThreshold)
            {
                if (openTime < sensorThreshold) // erster Intervall
                {
                    return sensorThreshold - openTime;
                }
                else if (openTime <= TimeSpan.FromMinutes(17))
                {
                    return TimeSpan.FromMinutes(1); // 1 Minuten wenn Gesamtzeit unter 15  minuten
                }
                else if (openTime <= TimeSpan.FromMinutes(32))
                {
                    return TimeSpan.FromMinutes(15); // 15 Minuten wenn Gesamtzeit unter 30  minuten
                }
                else if (openTime <= TimeSpan.FromMinutes(62))
                {
                    return TimeSpan.FromMinutes(30); // 30 Minuten wenn Gesamtzeit unter 60  minuten
                }
                else if (openTime <= TimeSpan.FromMinutes(122))
                {
                    return TimeSpan.FromMinutes(60); // 60 Minuten wenn Gesamtzeit unter 120  minuten
                }
                else if (openTime <= TimeSpan.FromMinutes(242))
                {
                    return TimeSpan.FromMinutes(120); // 120 Minuten wenn Gesamtzeit unter 240  minuten
                }
                else if (openTime <= TimeSpan.FromMinutes(482))
                {
                    return TimeSpan.FromMinutes(240); // 240 Minuten wenn Gesamtzeit unter 480  minuten
                }
                else
                {
                    /* Bereits über Schwellwert -> relativ lange Pause, bevor nächste Warnung ausgegeben wird */
                    return TimeSpan.FromHours(8); // 8 Stunden
                }
            }

            private async Task<TurnedOffHeatingInfo[]> TurnOffAssociatedHeatings(CancellationToken cancellationToken = default)
            {
                /* Wenn Heizkörper zugeordnet sind und noch nicht ausgeschaltet wurden... */
                List<TurnedOffHeatingInfo> heatingsTurnedOff = new List<TurnedOffHeatingInfo>();
                if (this.AssociatedHeatings?.Any() == true && !this.OriginalHeatingTemperatures.Any())
                {
                    foreach (byte heatingId in this.AssociatedHeatings)
                    {
                        try
                        {
                            DeviceDto? heatingDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync(heatingId);
                            if (heatingDevice?.Thermostat?.TemperatureSetpoint is float setpoint)
                            {
                                /* 12° bedeutet "aus" - nur ausschalten wenn nicht sowieso schon aus! */
                                if (setpoint > DEFAULT_OFF_TEMPERATURE)
                                {
                                    this.OriginalHeatingTemperatures.Add(new KeyValuePair<byte, float>(heatingId, setpoint));

                                    await _deviceServiceClient.SetThermostatTemperatureAsync(heatingDevice.Id, DEFAULT_OFF_TEMPERATURE);

                                    heatingsTurnedOff.Add(new TurnedOffHeatingInfo
                                    {
                                        HeatingNodeId = heatingId,
                                        HeatingName = heatingDevice.Name!,
                                        HeatingRoom = heatingDevice.RoomId.HasValue
                                            ? _roomSnapshot.FirstOrDefault(r => r.Id == heatingDevice.RoomId.Value)?.Name ?? ""
                                            : ""
                                    });
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Heizkörper {HeatingId} nicht gefunden oder kein Thermostat", heatingId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Fehler beim Ausschalten der Heizung {HeatingId}", heatingId);
                        }
                    }
                }

                return heatingsTurnedOff.ToArray();
            }

            internal void UpdateOriginalHeatingTemperature(byte nodeID, byte temperatureSetpoint)
            {
                var entriesToUpdate = this.OriginalHeatingTemperatures.Where(item => item.Key == nodeID).ToList();
                this.OriginalHeatingTemperatures.RemoveAll(item => item.Key == nodeID);

                if (entriesToUpdate.Any())
                {
                    /* Es kann nur einen geben (s.o.)! */
                    this.OriginalHeatingTemperatures.Add(new KeyValuePair<byte, float>(nodeID, temperatureSetpoint));

                }
                else
                {
                    /* Bisher noch keine Zieltemp (z.B. weil vorher aus war), aber Heizung sollte überwacht werden
                     * -> neuen Zielwert hinterlegen...
                     */
                    this.OriginalHeatingTemperatures.Add(new KeyValuePair<byte, float>(nodeID, temperatureSetpoint));

                }
            }
        }
    }
}
