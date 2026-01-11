using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;
using Microsoft.Extensions.Logging;

namespace Sarah.Monitoring.Monitors
{
    public class PersonMonitor(IDBService _db, IEventProcessingService _events, ILogger<PersonMonitor> _logger) : ICanSelfTest, IMonitor
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
                this.UpdateCancellationTokenSource?.Cancel();
            }
        }


        private Task? UpdateTask { get; set; }
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


            _logger.LogDebug("PersonMonitor gestartet.");

            return Task.CompletedTask;
        }

        private async Task Update()
        {
            try
            {
                foreach (PersonInfo person in _db.Persons)
                {
                    if (person != null)
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
                            await _events.PublishPersonAvailabilityAsync(new PersonAvailabilityEvent(person.Id, person?.Name ?? "", currentState));
                        }

                        /* Person ist nicht daheim -> Suchen, ob sie sich in einem GeoFence befindet oder im Vergleich zum letzten Mal einen Verlassen hat */


                        IGeoFence? lastFence, currentFence = null;
                        currentFence = person!.CurrentGeoFence;

                        if (!GeoFencesByPersonId.TryGetValue(person.Id, out lastFence))
                        {
                            lastFence = null;
                            GeoFencesByPersonId.Add(person.Id, lastFence);
                        }

                        if (lastFence != currentFence)
                        {
                            /* GeoFence Der Person hat sich geändert -> Event auslösen! */
                            GeoFencesByPersonId[person.Id] = currentFence;
                            await _events.PublishGeoFenceEventAsync(new PersonGeoFenceEvent(person.Id, person?.Name ?? "", currentFence, lastFence));
                        }
                    }
                }
                this._lastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Fehler beim Aktualisieren der Personenzustände: " + ex.Message);
            }
        }


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
