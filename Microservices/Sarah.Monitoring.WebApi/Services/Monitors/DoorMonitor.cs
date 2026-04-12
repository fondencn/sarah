using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;
using Sarah.ServiceClients;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Steuerungs- und Überwachungsfunktionen für geöffnete Türen und Fenster.
    /// Hier sind alle NodeIds für Christians Wohnung fest verdrahtet!
    /// </summary>
    public class DoorMonitor (IReadOnlyList<RoomDto> _roomSnapshot, IWeatherProvider _weather, RabbitMQClient _rabbitMQ, ILogger<DoorMonitor> _logger, DeviceServiceClient _deviceServiceClient) : ICanSelfTest, IMonitor, IDoorMonitor
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
        /// <summary>
        /// Lautere Sprachausgabe bei folgenden Türen
        /// </summary>
        public static byte[] WarnLouderNodeIds { get; } = new byte[] {34 };


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

            // 2) Door monitoring subscribes to the specific door-state topic introduced for typed events.
            await _rabbitMQ.SubscribeAsync<DoorSensorStateChangedMessage>(
                topic: MessageTopics.NetworkEventsDoorState,
                onMessage: message => Update(message.SourceNodeId),
                exchange: MessageTopics.NetworkEvents);

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

                            // Pre-resolve heating device DB IDs and room names for speech
                            var heatingInfo = new Dictionary<byte, HeatingInfo>();
                            if (associatedHeatings != null)
                            {
                                foreach (byte hid in associatedHeatings)
                                {
                                    DeviceDto? hDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync(hid);
                                    if (hDevice != null)
                                    {
                                        RoomDto? hRoom = hDevice.RoomId.HasValue
                                            ? _roomSnapshot.FirstOrDefault(r => r.Id == hDevice.RoomId.Value)
                                            : null;
                                        heatingInfo[hid] = new HeatingInfo(hDevice.Id, hRoom?.Name);
                                    }
                                }
                            }

                            this.CurrentOpenDoorTasks.Add(new SurveillanceTask(liveDevice, sensorThreshold, room, warnAtOpen, associatedHeatings, _rabbitMQ, heatingInfo, this, _weather, _logger, _deviceServiceClient, sensorState.LastStateChanged.Value));
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
                .Where (item => item.AssociatedHeatings?.Contains(nodeID) == true)
                .ToList()
                .ForEach(item => item.UpdateOriginalHeatingTemperature(nodeID, temperatureSetpoint));

        /// <summary>
        /// Ein einzelner Monitoring-Task für ein aktuell geöffnetes Fenster,
        /// der beendet wird, sobald das zugehörige Fenster wieder geschlossen wird.
        /// </summary>
        private sealed record HeatingInfo(long DbId, string? RoomName);

        private class SurveillanceTask
        {
            private readonly RabbitMQClient _rabbitMQ;
            private readonly IReadOnlyDictionary<byte, HeatingInfo> _heatingInfo;
            private readonly DoorMonitor _doorMonitor;
            private readonly IWeatherProvider _weather;
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
            public SurveillanceTask(DeviceDto device, TimeSpan sensorThreshold, RoomDto? room, bool warnAtOpen, byte[]? associatedHeatings, RabbitMQClient rabbitMQ, IReadOnlyDictionary<byte, HeatingInfo> heatingInfo, DoorMonitor doorMonitor, IWeatherProvider weather, ILogger<DoorMonitor> logger, DeviceServiceClient deviceServiceClient, DateTime openedAt)
            {
                this._rabbitMQ = rabbitMQ;
                this._heatingInfo = heatingInfo;
                this._doorMonitor = doorMonitor;
                this._weather = weather;
                this._logger = logger;
                this._deviceServiceClient = deviceServiceClient;
                this._openedAt = openedAt;
                this.Device = device;
                this.Room = room;
                this.SensorThreshold = sensorThreshold;
                this.WarnAtOpen = warnAtOpen;
                this.AssociatedHeatings = associatedHeatings;

                CancellationTokenSource cts = new CancellationTokenSource();
                this.UpdateCancellationTokenSource = cts;

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
                int lastMinutes = -1;
                TimeSpan waitTime = this.SensorThreshold;
                bool isInitialLoop = true;
                string artikel = this.Device.Name!.IndexOf("Fenster", StringComparison.OrdinalIgnoreCase) >= 0 ?
                    "Das" : "Die";

                while (!UpdateCancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (this.WarnAtOpen && isInitialLoop)
                    {
                        await _rabbitMQ.PublishAsync(new SayMessage(artikel + " " + this.Device.Name + " wurde geöffnet."));
                        isInitialLoop = false;
                    }

                    try
                    {
                        await Task.Delay(waitTime, this.UpdateCancellationTokenSource.Token);
                    }
                    catch (System.Threading.Tasks.TaskCanceledException) { /* Weiter laufen lassen, das bedeutet nur dass die Tür wieder zu ist */ }

                    DeviceDto? liveDevice = await _deviceServiceClient.GetDeviceByNodeIdAsync((byte)this.Device.NodeId);
                    DoorSensorStateDto? sensorState = liveDevice?.DoorSensor;
                    TimeSpan openTime = DateTime.Now - _openedAt;


                    if (!UpdateCancellationTokenSource.Token.IsCancellationRequested)
                    {

                        if (openTime >= this.SensorThreshold)
                        {
                            int minutes = (int)openTime.TotalMinutes;
                            if (minutes != lastMinutes)
                            {
                                lastMinutes = minutes;
                                string sayMsg;
                                if (minutes == 0)
                                {
                                    sayMsg = artikel + " " + this.Device.Name + " ist seit kurzem offen.";
                                }
                                else if (minutes == 1)
                                {
                                    sayMsg = artikel + " " + this.Device.Name + " ist eine Minute offen.";
                                }
                                else
                                {
                                    sayMsg = artikel + " " + this.Device.Name + " ist " + minutes + " Minuten offen. ";
                                    if (sensorState?.State == DoorSensorState.Offen)
                                    {
                                        if (openTime >= TimeSpan.FromMinutes(15) && openTime < TimeSpan.FromMinutes(20) && waitTime < TimeSpan.FromMinutes(15))
                                        {
                                            sayMsg += $"Die nächste Information erfolgt in 15 Minuten. ";
                                            waitTime = TimeSpan.FromMinutes(15);
                                        }

                                        else if (openTime >= TimeSpan.FromMinutes(30) && openTime < TimeSpan.FromMinutes(65) && waitTime < TimeSpan.FromMinutes(30))
                                        {
                                            sayMsg += $"Die nächste Information erfolgt in 30 Minuten. ";
                                            waitTime = TimeSpan.FromMinutes(30);
                                        }

                                        else if (openTime >= TimeSpan.FromMinutes(60) && openTime < TimeSpan.FromMinutes(125) && waitTime < TimeSpan.FromMinutes(60))
                                        {
                                            sayMsg += $"Die nächste Information erfolgt in einer Stunde. ";
                                            waitTime = TimeSpan.FromMinutes(60);
                                        }

                                        else if (openTime >= TimeSpan.FromMinutes(120) && openTime < TimeSpan.FromMinutes(185) && waitTime < TimeSpan.FromMinutes(120))
                                        {
                                            sayMsg += $"Die nächste Information erfolgt in zwei Stunden. ";
                                            waitTime = TimeSpan.FromMinutes(120);
                                        }

                                        else if (openTime >= TimeSpan.FromMinutes(240) && openTime < TimeSpan.FromMinutes(245) && waitTime < TimeSpan.FromMinutes(240))
                                        {
                                            sayMsg += $"Die nächste Information erfolgt in vier Stunden. ";
                                            waitTime = TimeSpan.FromMinutes(240);
                                        }
                                    }
                                }


                                /* Raumtemperatur ansagen */
                                if (this.Room != null)
                                {
                                    try
                                    {
                                        float? avgTemp = await _deviceServiceClient.GetRoomAverageTemperatureAsync(this.Room.Id);
                                        if (avgTemp.HasValue)
                                        {
                                            if (avgTemp.Value < 18f)
                                                sayMsg += $" Die Raumtemperatur beträgt nur noch {avgTemp.Value:F1} Grad.";
                                            else if (avgTemp.Value > 26f)
                                                sayMsg += $" Die Raumtemperatur beträgt {avgTemp.Value:F1} Grad.";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Raumtemperatur konnte nicht abgefragt werden");
                                    }
                                }

                                /* Wenn Heizkörper zugeordnet sind und noch nicht ausgeschaltet wurden... */
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
                                                if (setpoint != 12.0f)
                                                {
                                                    this.OriginalHeatingTemperatures.Add(new KeyValuePair<byte, float>(heatingId, setpoint));

                                                    /* Heizung ohne await damit die Sprachausgabe sofort kommt
                                                     * Könnte zum Problem bei mehreren Heizungen werden (ZWave-RaceCondition!) */
                                                    _ = _deviceServiceClient.SetThermostatTemperatureAsync(heatingDevice.Id, 12.0f);
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

                                    if (this.OriginalHeatingTemperatures.Any())
                                    {
                                        var roomNames = this.OriginalHeatingTemperatures
                                            .Select(kv => _heatingInfo.TryGetValue(kv.Key, out HeatingInfo? info) ? info.RoomName : null)
                                            .Where(n => n != null).Distinct().ToList();
                                        sayMsg += " Heizung" + (this.OriginalHeatingTemperatures.Count > 1 ? "en" : "")
                                            + " im " + string.Join(" und ", roomNames) + " ausgeschalt"
                                            + (this.OriginalHeatingTemperatures.Count > 1 ? "en" : "et");
                                    }

                                }
                                if (DoorMonitor.WarnLouderNodeIds.Contains((byte)this.Device.NodeId))
                                {
                                    /* Bei der Haustüre Lautere Sprachausgabe */
                                    await _rabbitMQ.PublishAsync(new SayMessage(sayMsg, "", Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.VeryLoud));
                                } 
                                else 
                                {
                                    /* Normale Sprachausgabe */
                                    await _rabbitMQ.PublishAsync(new SayMessage(sayMsg));
                                }
                            }
                        }
                    }
                    else
                    {
                        string sayMsg;
                        if (openTime > this.SensorThreshold)
                        {
                            sayMsg = artikel + " " + this.Device.Name + " ist geschlossen. ";
                        }
                        else
                        {
                            sayMsg = artikel + " " + this.Device.Name + " ist kurzzeitig offen gewesen. ";
                        }

                        if (this.OriginalHeatingTemperatures.Any())
                        {
                            List<string> heatingMsgParts = new List<string>();
                            foreach (var origTemp in this.OriginalHeatingTemperatures)
                            {
                                bool isAnotherWindowOpen = _doorMonitor.GetWindowTrackingsForHeating(origTemp.Key)
                                    .Any(t => t.Device.Id != this.Device.Id);

                                _heatingInfo.TryGetValue(origTemp.Key, out HeatingInfo? info);
                                string roomLabel = info?.RoomName ?? origTemp.Key.ToString();

                                if (!isAnotherWindowOpen)
                                {
                                    if (info != null)
                                        _ = _deviceServiceClient.SetThermostatTemperatureAsync(info.DbId, origTemp.Value);
                                    heatingMsgParts.Add($" im {roomLabel} auf {origTemp.Value:F1} Grad gestellt");
                                }
                                else
                                {
                                    heatingMsgParts.Add($" im {roomLabel} bleibt aus wegen anderes offenes Fenster");
                                }
                            }

                            if (heatingMsgParts.Any())
                            {
                                sayMsg += " Heizung" + (this.OriginalHeatingTemperatures.Count > 1 ? "en" : "")
                                    + string.Join(" und", heatingMsgParts);
                            }
                        }

                        await _rabbitMQ.PublishAsync(new SayMessage(sayMsg));

                        //Ende der Taskausführung!
                        break;
                    }
                }
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
