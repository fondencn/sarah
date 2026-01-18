using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.Monitoring.WebApi.Data;
using Sarah.Monitoring.WebApi.Data.Entities;
using Sarah.Monitoring.WebApi.Extensions;
using Sarah.API.Interfaces.Services;
using Sarah.API.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Steuerungs- und Überwachungsfunktionen für geöffnete Türen und Fenster.
    /// Hier sind alle NodeIds für Christians Wohnung fest verdrahtet!
    /// </summary>
    public class DoorMonitor (ApplicationDbContext _db, IEventProcessingService _events, IDeviceService _devices, IWeatherProvider _weather, ILogger<DoorMonitor> _logger) : INetworkEventSubscriber, ICanSelfTest, IMonitor, IDoorMonitor
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
            { 2,  new byte[]{ 40    } },  // Terassentür Arbeitszimmer macht Heizung im Wohnzimmer aus
            { 23, new byte[]{ 37    } },  // Fenster Lukaszimmer macht Heizung im Lukaszimmer aus
            { 22, new byte[]{ 40    } },  // Fenster Wohnzimmer macht Heizung im Wohnzimmer aus
            { 44, new byte[]{ 42,19 } },  // Fenster Schlafzimmer macht Heizung im Schlafzimmer und im Bad aus
            { 33, new byte[]{ 45    } },  // Balkontür Wohnzimmer macht Heizung im Wohnzimmer aus
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
            if (!this.IsRunning)
            {
                await _events.SubscribeNetworkEventAsync(this);
                _logger.LogDebug("DoorMonitor gestartet.");
                this.IsRunning = true;
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn eine Device-Nachricht veröffentlicht wird
        /// </summary>
        /// <param name="changedNodeId">ID des geändertes Knotens</param>
        private async Task Update(byte changedNodeId)
        {
            try
            {
                DeviceInfoEntity? device = await _db.Devices.FirstOrDefaultAsync(item => item.NodeID  ==(long)changedNodeId);

                if (device != null)
                {
                    IDoorSensor? sensor = device.GetNetworkItem(_devices) as IDoorSensor;

                    if (sensor != null)
                    {
                        TimeSpan sensorThreshold;
                        if (!DoorOpenTimeThresholds.TryGetValue(device.NodeID, out sensorThreshold))
                        {
                            sensorThreshold = DefaultOpenTimeThreshold;
                        }

                        if (sensor.State == DoorSensorState.Offen)
                        {
                            /* Tür geöffnet -> Uberwachung starten */
                            if (!this.CurrentOpenDoorTasks.Any(task => task.Device.NodeID == changedNodeId)
                                && sensor.LastStateChanged.HasValue)
                            {
                                RoomEntity? room;
                                if (device.Id_Room.HasValue)
                                {
                                    room = await _db.Rooms.FindAsync(device.Id_Room);
                                }
                                else
                                {
                                    room = null;
                                }
                                bool warnAtOpen = this.WarnImmediateNodeIds.Contains(device.NodeID);

                                byte[]? associatedHeatings;
                                if (!DoorToHeatingsMapping.TryGetValue(device.NodeID, out associatedHeatings))
                                {
                                    associatedHeatings = null; // keine Heizung zu diesem Fenster konfiguriert...
                                }

                                this.CurrentOpenDoorTasks.Add(new SurveillanceTask(device, sensorThreshold, room, _db, warnAtOpen, associatedHeatings, _events, _devices, this, _weather, _logger));
                            }
                        }
                        else
                        {
                            /* Tür geschlossen, Überwachung beenden, kurze Sprachausgabe zur Info erzeugen */
                            SurveillanceTask? task = this.CurrentOpenDoorTasks.FirstOrDefault(task => task.Device.NodeID == changedNodeId);
                            if (task != null)
                            {
                                task.Cancel();
                                this.CurrentOpenDoorTasks.Remove(task);
                            }
                        }
                    } // else: Kein DoorSensor - nichts für diese Klasse zu tun
                    this.LastUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren der Türzustände");
            }
        }

        /// <summary>
        /// Notify (aus <c>INetworkEventSubscriber</c> )
        /// </summary>
        /// <param name="e">Nachrichtenereignis</param>
        /// <returns>Task</returns>
        public Task Notify(NetworkEvent e)
        {
            return Update(e.SourceNodeId);
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
        private class SurveillanceTask
        {
            private readonly ApplicationDbContext _db;
            private readonly IEventProcessingService _events;
            private readonly IDeviceService _devices;
            private readonly DoorMonitor _doorMonitor;
            private readonly IWeatherProvider _weather;
            private readonly ILogger<DoorMonitor> _logger;

            /// <summary>
            /// Der Türsensor
            /// </summary>
            public DeviceInfoEntity Device { get; private set; }

            /// <summary>
            /// zugeordneter Raum
            /// </summary>
            public RoomEntity? Room { get; private set; }

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
            public SurveillanceTask(DeviceInfoEntity device, TimeSpan sensorThreshold, RoomEntity? room, ApplicationDbContext db, bool warnAtOpen, byte[]? associatedHeatings, IEventProcessingService events, IDeviceService devices, DoorMonitor doorMonitor, IWeatherProvider weather, ILogger<DoorMonitor> logger)
            {
                this._db = db;
                this._events = events;
                this._devices = devices;
                this._doorMonitor = doorMonitor;
                this._weather = weather;
                this._logger = logger;
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
                        await _events.PublishSay(new SayEvent(artikel + " " + this.Device.Name + " wurde geöffnet."));
                        isInitialLoop = false;
                    }

                    try
                    {
                        await Task.Delay(waitTime, this.UpdateCancellationTokenSource.Token);
                    }
                    catch (System.Threading.Tasks.TaskCanceledException) { /* Weiter laufen lassen, das bedeutet nur dass die Tür wieder zu ist */ }

                    IDoorSensor sensor = (IDoorSensor)this.Device.GetNetworkItem(_devices);
                    TimeSpan openTime = sensor.LastOpenDuration.GetValueOrDefault();


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
                                    if (this.Room != null)
                                    {
                                        var temperatures = _db.Devices.Where(d => d.Id_Room == this.Room.Id)
                                            .ToList()
                                            .Select(item => item.GetNetworkItem(_devices))
                                            .OfType<ITemperatureSensor>()
                                            .Select(d => d.Temperature.Value)
                                            .DefaultIfEmpty(0);
                                        var avgTemp = temperatures.Sum() > 0 ? temperatures.Average(): 21;
                                        if (avgTemp < 18)
                                        {
                                            sayMsg += "Die Raumtemperatur beträgt nur noch " + avgTemp + "°C. ";
                                            if (_weather.CurrentOutdoorTemperature < 12)
                                            {
                                                sayMsg += "Draußen ist es kalt, Fenster bitte schließen. ";
                                            }
                                        }
                                        else if (avgTemp > 26)
                                        {
                                            sayMsg += "Die Raumtemperatur beträgt mehr als " + avgTemp + "°C. ";
                                            if (_weather.CurrentOutdoorTemperature > 27)
                                            {
                                                sayMsg += "Draußen ist es ziemlich heiß, Fenster bitte schließen. ";
                                            }
                                        }
                                    }



                                    if (sensor.State == DoorSensorState.Offen)
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


                                /* Wenn Heizkörper zugeordnet sind und noch nicht ausgeschaltet wurden... */
                                if (this.AssociatedHeatings?.Any() == true && !this.OriginalHeatingTemperatures.Any())
                                {
                                    foreach (byte heatingId in this.AssociatedHeatings)
                                    {
                                        IThermoElement? heating = _devices.Heatings.FirstOrDefault(item => item.NodeID == heatingId);
                                        if (heating != null)
                                        {
                                            /* 12° bedeutet "aus" - nur ausschalten wenn nicht sowieso schon aus! */
                                            if (heating.TemperatureSetpoint.Value != 12.0f)
                                            {
                                                this.OriginalHeatingTemperatures.Add(new KeyValuePair<byte, float>(heatingId, heating.TemperatureSetpoint.Value));

                                                /* Heizung ohne await damit die Sprachausgabe sofort kommt
                                                 * Könnte zum Problem bei mehreren Heizungen werden (ZWave-RaceCondition!) */
                                                _ = heating.SetTemperature(12.0f);
                                            }
                                        }
                                        else
                                        {
                                            throw new InvalidOperationException("Programm/Konfigurationsfehler: Heizkörper" + heatingId + " ist unbekannt!");
                                        }
                                    }

                                    if (this.OriginalHeatingTemperatures.Any())
                                    {
                                        var heatingdeviceInfos = _db.Devices.ToList().Where(d => this.AssociatedHeatings.Contains(d.NodeID)).ToList();
                                        var rooms = _db.Rooms.ToList().Where(r => heatingdeviceInfos.Any(d => d.Id_Room == r.Id)).ToList();

                                        sayMsg += " Heizung" + (this.OriginalHeatingTemperatures.Count > 1 ? "en" : "")
                                            + " im " + String.Join(" und ", rooms.Select(r => r.Name))  + " ausgeschalt" + (this.OriginalHeatingTemperatures.Count > 1 ? "en" : "et");
                                    }
                                }
                                if (DoorMonitor.WarnLouderNodeIds.Contains(this.Device.NodeID))
                                {
                                    /* Bei der Haustüre Lautere Sprachausgabe */
                                    await _events.PublishSay(new SayEvent(sayMsg, vol: SpeechVolume.VeryLoud));
                                } 
                                else 
                                {
                                    /* Normale Sprachausgabe */
                                    await _events.PublishSay(new SayEvent(sayMsg));
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

                        /* Wenn die gekoppelte Heizung bei öffnen an war, dann jetzt wieder einschalten */
                        if (this.OriginalHeatingTemperatures.Any())
                        {
                            List<DeviceInfoEntity> heatingdeviceInfos = _db.Devices
                                .ToList()
                                .Where(d => this.AssociatedHeatings?.Contains(d.NodeID) == true)
                                .ToList();
                            List<RoomEntity> rooms = _db.Rooms
                                .ToList()
                                .Where(r => heatingdeviceInfos.Any(d => d.Id_Room == r.Id))
                                .ToList();

                            List<string> heatingMsg = new List<string>();
                            foreach(var origTemp in this.OriginalHeatingTemperatures)
                            {
                                DeviceInfoEntity heating = heatingdeviceInfos.First(item => item.NodeID == origTemp.Key);
                                RoomEntity room = rooms.First(item => item.Id == heating.Id_Room);

                                bool isAnotherWindowOpen = _doorMonitor.GetWindowTrackingsForHeating(origTemp.Key)
                                    .Any(item => item.Device.Id != this.Device.Id);

                                /* Heizung nur an machen, wenn nicht noch ein anderes Fenster offen ist, welches mit dieser
                                 * Heizung gekoppet ist
                                 */
                                if (!isAnotherWindowOpen)
                                {
                                    /* Heizung ohne await damit die Sprachausgabe sofort kommt
                                     * Könnte zum Problem bei mehreren Heizungen werden (ZWave-RaceCondition!)
                                     */
                                    _ = ((IThermoElement)heating.GetNetworkItem(_devices)).SetTemperature(origTemp.Value);
                                    heatingMsg.Add(" im " + room.Name + " auf " + origTemp.Value + " °C gestellt");
                                }
                                else
                                {
                                    heatingMsg.Add(" im " + room.Name + " bleibt aus, weil noch ein anderes Fenster offen ist ");

                                }
                            }
                            sayMsg += " Heizung" + (OriginalHeatingTemperatures.Count > 1 ? "en" : "")
                                + String.Join(" und ", heatingMsg);
                        }

                        await _events.PublishSay(new SayEvent(sayMsg));

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
