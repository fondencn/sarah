using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Logging;

namespace Sarah.Monitoring.Monitors
{
    public class PersonMonitor (IDBService _db, IEventProcessingService _events, IGeoFenceService _geofences) : ICanSelfTest
    {
        private readonly object DBLock = new object();

        private DateTime _lastUpdate;



        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~PersonMonitor()
        {
            if (this.UpdateTask != null && this.UpdateTask.Status == TaskStatus.Running)
            {
                this.UpdateCancellationTokenSource.Cancel();
            }
        }


        private Task UpdateTask { get; set; }
        private CancellationTokenSource? UpdateCancellationTokenSource { get; set; }

        private Dictionary<long, bool> ConnectionStatesByPersonId { get; } = new Dictionary<long, bool>();
        private Dictionary<long, IGeoFence?> GeoFencesByPersonId { get; } = new Dictionary<long, IGeoFence?>();

        public Task Start()
        {


            CancellationTokenSource cts = new CancellationTokenSource();
            this.UpdateCancellationTokenSource = cts;
            this.UpdateTask = Task.Run(async () =>
            {
                while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Update();
                    /* Alle 60 Sekunden */
                    await Task.Delay(60000);
                }
            }, cts.Token);


            Logger.Instance.LogDebug("PersonMonitor gestartet.");

            return Task.CompletedTask;
        }

        private async Task Update()
        {
            try
            {
                foreach (var person in _db.Persons)
                {
                    bool lastState;
                    if (!ConnectionStatesByPersonId.TryGetValue(person.Id, out lastState))
                    {
                        lastState = false;
                        ConnectionStatesByPersonId.Add(person.Id, lastState);
                    }

                    bool currentState = person.IsAtHome; // das hier wird vom Router geladen (Gerät ist im LAN oder nicht)
                    bool changed = currentState != lastState;

                    if (changed)
                    {
                        ConnectionStatesByPersonId[person.Id] = currentState;
                        await _events.PublishPersonAvailabilityAsync(new PersonAvailabilityEvent(person.Id, person.Name, currentState));
                    }

                        /* Person ist nicht daheim -> Suchen, ob sie sich in einem GeoFence befindet oder im Vergleich zum letzten Mal einen Verlassen hat */

                
                        IGeoFence? lastFence, currentFence = null;
                        currentFence = person.CurrentGeoFence;

                        if (!GeoFencesByPersonId.TryGetValue(person.Id, out lastFence))
                        {
                            lastFence = null;
                            GeoFencesByPersonId.Add(person.Id, lastFence);
                        }

                        if (lastFence != currentFence)
                        {
                            /* GeoFence Der Person hat sich geändert -> Event auslösen! */
                            GeoFencesByPersonId[person.Id] = currentFence;
                            await _events.PublishGeoFenceEventAsync(new PersonGeoFenceEvent(person.Id, person.Name, currentFence, lastFence));
                        }
                }
                this._lastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogDebug("Fehler beim Aktualisieren der Personenzustände: " + ex.Message);
            }
        }

        // /// <summary>
        // /// Gibt an, ob die Person mit dem angegebenen Namen aktuell zuhause ist. 
        // /// Dies erfolgt über das verbundene Mobiltelefon und über den zugeordneten GPS Tracker. 
        // /// 
        // /// Wenn eines der beiden Geräte im Zuhause Geofence ist, dann gilt die Person als Anwesend. 
        // /// </summary>
        // /// <param name="personName">Name der gesuchten Person</param>
        // /// <returns>true wenn das Mobiltelefon oder der GPS Tracker der Person zu Hause ist</returns>
        // public bool IsPresent(string personName)
        // {
        //     lock (DBLock)
        //     {
        //         bool res;
        //         try
        //         {
        //             var person = _db.Persons.AsEnumerable().FirstOrDefault(item => String.Equals(item.Name, personName, StringComparison.OrdinalIgnoreCase));


        //             res = InteLuk.HomeNet.HomeNetwork.Instance.KnownHosts?
        //                 .Any(item => String.Equals(item.Hostname, person.MobilePhoneHostname, StringComparison.OrdinalIgnoreCase)
        //                     && item.IsConnected) == true;

        //             if (person.GPSTrackerID != 0)
        //             {
        //                 var trackerDevice = _db.Devices.First(item => item.Id == person.GPSTrackerID);
        //                 IGPSTracker tracker = trackerDevice.NetworkElement as IGPSTracker;
        //                 if (tracker != null && tracker.Position?.IsValid == true)
        //                 {
        //                     bool isTrackerAtHome = GeoFences.GetCurrent(tracker.Position) == GeoFences.Zuhause;
        //                     res |= isTrackerAtHome;
        //                 }
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             Logger.Instance.LogDebug("Fehler beim Abfragen  der Präsenz von " + personName + ": " + ex.Message);
        //             res = false;
        //         }
        //         return res;
        //     }
        // }

        // public bool IsSomeonePresent()
        // {
        //     bool isDeviceInHomeWifi = this.ConnectionStatesByPersonId.Count > 0 && this.ConnectionStatesByPersonId.Any(entry => entry.Value == true);
        //     bool isTrackerAtHome = InteLukNetwork.GetSingletonInstance().GPSTrackers.Any(tracker => GeoFences.GetCurrent(tracker.Position) == GeoFences.Zuhause);

        //     return isDeviceInHomeWifi || isTrackerAtHome;
        // }

        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (this.ConnectionStatesByPersonId.Count == 0)
            {
                yield return new SelfTestResult(true, "Personen-Überwachung", "Keine Informationen über anwesende Personen geladen");
            }
            if ((DateTime.Now - this._lastUpdate) > TimeSpan.FromDays(2))
            {
                yield return new SelfTestResult(true, "Personen-Überwachung", "Die Daten sind älter als 2 Tage");
            }
        }



    }
}
