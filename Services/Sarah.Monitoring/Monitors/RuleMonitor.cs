using System.Collections.ObjectModel;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;
using Sarah.Logging;
using Sarah.Rules.Actions;
using Sarah.Rules.Conditions;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Verbindet die Regeln aus der Datenbank mit der eigentlichen RuleEngine und
    /// aktualisiert diese, wenn sich die Datenbankeinträge ändern
    /// </summary>
    public class RuleMonitor (
        IRuleService _ruleService, 
        IFerienInfoProvider _ferien, 
        IEventProcessingService _events, 
        IDBService _db, 
        IDeviceService _devices, 
        IDoorMonitor _doors,
        IWeatherProvider _weather) 

        : IRuleStore, ICanSelfTest, IMonitor
    {

        private string? _aktuelleFerien;
        private Task? UpdateTask { get; set; }
        private CancellationTokenSource? UpdateCancellationTokenSource { get; set; }
        private ReadOnlyCollection<Rule>? _rules = null;


        /// <summary>
        /// Die erstellen Regeln für die RuleEngine
        /// </summary>
        public IReadOnlyCollection<Rule> Rules
        {
            get
            {
                if (_rules == null)
                {
                    _rules = CreateTemperatureScheduleRulesFromDb(_db, _devices, _doors, _weather)
                        .Concat(CreateAlarmScheduleRulesFromDb(_db, _events))
                        .ToList()
                        .AsReadOnly();
                }
                return _rules;
            }
            private set
            {
                _rules = new ReadOnlyCollection<Rule>(value.ToList());
            }
        }



        /// <summary>
        /// Wird ausgelöst, wenn die Regeln sich geändert haben und daher von der RuleEngine neu eingelesen werden sollen.
        /// </summary>
        public event EventHandler? Changed;


        /// <summary>
        /// dtor
        /// </summary>
        ~RuleMonitor()
        {
            this.UpdateCancellationTokenSource?.Cancel();
        }


        /// <summary>
        /// Startet die Überwachung für Regeln aus der Datenbank
        /// und synchronisiert diese mit der RuleEngine.
        ///
        /// Dabei werden Termine überwacht.
        ///
        /// Startet einen Timer, der alle 12 Stunden überprüft,
        /// ob  Ferien sind und  dann die entsprechenden
        /// Termine aktiviert bzw. deaktiviert.
        /// </summary>
        /// <returns>Task</returns>
        public Task Start()
        {
            _ruleService.RegisterRuleStore(this);
            Logger.Instance.LogDebug("RuleMonitor gestartet.");


            CancellationTokenSource cts = new CancellationTokenSource();
            this.UpdateCancellationTokenSource = cts;
            this.UpdateTask = Task.Run(async () =>
            {
                /* Kurz warten und dann Termine an/aus schalten */
                await Task.Delay(10 * 1000);
                await UpdateActiveInActiveAppointments();

                while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
                {
                    /* Alle 12 Stunden den Status der Alarme überprüfen und an/aus schalten */
                    await Task.Delay(12 * 60 * 1000);
                    await UpdateActiveInActiveAppointments();
                }
            }, cts.Token);

            return Task.CompletedTask;
        }

        private async Task UpdateActiveInActiveAppointments()
        {
            await CleanupOldAlarms();
            await this.RaiseChanged();
        }

        /// <summary>
        /// Wendet die Einstellungen für die Ferien auf alle Erinnerungen
        /// in der Datenbank an und schaltet die IsActive Eigenschaft entsprechend an oder aus.
        /// </summary>
        /// <returns>Task</returns>
        private async Task ApplyFerien()
        {
            try
            {
                string? aktuelleFerien = _ferien.AktuelleFerien?.Name;
                bool sindGeradeFerien = aktuelleFerien != null;

                if (this._aktuelleFerien == null && aktuelleFerien != null)
                {
                    /* Ferienbeginn erkannt */
                    await _events.PublishSay(new SayEvent("Heute sind Ferien: " + aktuelleFerien));
                }
                if (this._aktuelleFerien != null && aktuelleFerien == null)
                {
                    /* Ferienende erkannt */
                    await _events.PublishSay(new SayEvent("Ende der Ferien: " + aktuelleFerien));
                }


                this._aktuelleFerien = aktuelleFerien;


                foreach (var item in _db.AlarmSchedule
                    .Where(item => item.IsNichtInFerien == true || item.IsNurInFerien == true)
                    .ToList())
                {
                    bool prevIsActive = item.IsActive;
                    if (item.IsNichtInFerienNotNull)
                    {
                        item.IsActive = !sindGeradeFerien;
                    }
                    if (item.IsNurInFerienNotNull)
                    {
                        item.IsActive = sindGeradeFerien;
                    }
                    if (prevIsActive != item.IsActive)
                    {
                        _db.AlarmSchedule.Update(item);
                        await _db.SaveChangesAsync();
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim deaktiveren alter Erinnerungen", ex);
            }
        }

        /// <summary>
        /// Räumt alte Erinnerungen weg, die seit dem letzten Start vergessen wurden
        /// (noch aktiv, aber schon in der Vergangenheit).
        ///
        /// Serientermine bleiben aber aktiv.
        ///
        /// TODO: ggf. komplett aus der DB löschen, da diese sowieso nie wieder auftreten werden?
        /// </summary>
        /// <returns></returns>
        private async Task CleanupOldAlarms()
        {
            try
            {
                foreach (var item in _db.AlarmSchedule
                    .Where(item => item.AlarmTime <= DateTime.Now && !item.HasRecurrence && item.IsActive)
                    .ToList())
                {
                    item.IsActive = false;
                    _db.AlarmSchedule.Update(item);
                    await _db.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Fehler beim deaktiveren alter Erinnerungen", ex);
            }
        }



        private static IEnumerable<Rule> CreateAlarmScheduleRulesFromDb(IDBService db, IEventProcessingService events)
        {
            List<Rule> rules;
            try
            {
                var activerules = db.AlarmSchedule
                    .Where(item => item.IsActive)
                    .ToList();
                rules = activerules
                    .Select(item => CreateAlarmScheduleRules(item, db, events))
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Exception beim erzeugen der Rules für AlarmSchedules aus der Datenbank, daher keine Regel verfügbar", ex);
                Logger.Instance.LogError(ex.StackTrace);
                rules = new List<Rule>();
            }
            return rules;
        }

        private static Rule CreateAlarmScheduleRules(AlarmSchedule item, IDBService db, IEventProcessingService events)
        {
            try
            {
                RuleCondition condition;
                string name;

                if (item.HasRecurrence)
                {
                    TimerRecurrence tr = CreateRecurrence(item);
                    condition = new TimerCondition(tr);
                    name = "Erinnerung " + tr.GetNext().ToString("dd.MM.yyyy HH:mm") + ": " + (item.Text ?? "kein Text");
                }
                else
                {
                    condition = new TimerCondition(item.AlarmTime);
                    name = "Erinnerung " + item.AlarmTime.ToString("dd.MM.yyyy HH:mm") + ": " + (item.Text ?? "kein Text"); ;
                }


                return new Rule()
                {
                    /* Wiederholung oder einmaliger Termin? */
                    Condition = condition,
                    Action = new SayAction(
                        "Erinnerung um " + item.AlarmTime.ToString("HH:mm") + ": " + item.Text, 
                        String.IsNullOrWhiteSpace(item.TargetSpeaker) ? "" : item.TargetSpeaker, 
                        events,
                        item.Volume),
                    Name = name
                };
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError(ex.StackTrace);
                throw new InvalidOperationException("Fehler für Erinnerung " + item.Text + ": " + ex.Message, ex);
            }
        }


        /// <summary>
        /// Erzeugt Regeln für alle Räume und deren Heizungen an Hand der Datenbankeinträge
        /// in der Tabelle TemperatureSchedules
        /// </summary>
        /// <returns>Liste mir RuleEngine-Konfigurationen</returns>
        private static IEnumerable<Rule> CreateTemperatureScheduleRulesFromDb(IDBService db, IDeviceService devices, IDoorMonitor doors, IWeatherProvider weather)
        {
            List<Rule> rules;
            try
            {
                var activerules = db.TemperatureSchedules
                    .Where(item => item.IsActive)
                    .ToList();
                rules = activerules
                    .Select(item => CreateTemperatureScheduleRules(item, db, devices, doors, weather))
                    .SelectMany(item => item)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogDebug("Exception beim erzeugen der Rules für TemperatureSchedules aus der Datenbank: " + ex.Message);
                Logger.Instance.LogDebug("Daher keine Regeln verfügbar!");
                rules = new List<Rule>();
            }
            return rules;
        }

        /// <summary>
        /// Erzeugt Heizungsregeln für alle Heizungen im im TemperatureSchedule konfdigurierten Raum
        /// </summary>
        /// <param name="ts">TemperatureSchedule aus der Datenbank</param>
        /// <param name="context">DB-Context</param>
        /// <returns>Liste mit RuleGnine-Steuenrgsdaten für diesen Eintrag</returns>
        private static ICollection<Rule> CreateTemperatureScheduleRules(TemperatureSchedule ts, IDBService context, IDeviceService devices, IDoorMonitor doors, IWeatherProvider weather)
        {
            List<Rule> result = new List<Rule>();
            var devicesInRoom = context.Devices
                .Where(item => item.Id_Room == ts.Id_Room)
                .ToList();
            foreach (var heating in devicesInRoom.Where(item => item.GetNetworkItem(devices) is IThermoElement))
            {
                result.Add(new Rule()
                {
                    Condition = new TimerCondition(CreateRecurrence(ts)),
                    Action = CreateAction(ts, heating, devices, doors, weather),
                    Name = CreateName(ts, heating)
                });
            }
            return result;
        }

        /// <summary>
        /// Erzeugt einen ANzeigenamen für angegebenen Daten
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="heating"></param>
        /// <returns></returns>
        private static string CreateName(TemperatureSchedule ts, DeviceInfo heating)
        {
            string action = string.Format("Heizung {0} auf {1}°C stellen", heating.NodeID, ts.TemperatureSetpoint);

            return string.Format("{0} {1}", ts.DisplayTime, action);
        }

        /// <summary>
        /// Erzeugt eine RuleEngine-Action für die angegebenen Daten
        /// </summary>
        /// <param name="ts">Temperaturplanungselement</param>
        /// <param name="heating">Betroffenes Thermosntatgerät</param>
        /// <returns>RuleAction für die RUleEngine</returns>
        private static RuleAction CreateAction(TemperatureSchedule ts, DeviceInfo heating, IDeviceService devices, IDoorMonitor doors, IWeatherProvider weather)
        {
            RuleAction action = new ActionRuleAction(async (e) =>
            {
                try
                {
                    IThermoElement device = (IThermoElement)heating.GetNetworkItem(devices);
                    bool isHeatingOffBecauseWindowOpen = doors.IsWindowForHeatingTracking(device.NodeID);

                    if (isHeatingOffBecauseWindowOpen)
                    {
                        /* Zugeordnetes Fenster ist gerade geöffnet
                         * -> Window Tracking aktualisieren: Neue Schließ-Temperatur merken
                         * -> Nur wenn nicht gerade Sommerzeit ist
                         */
                        if (!TemperatureSchedule.IsSummertimeOffDate(DateTime.Today))
                        {
                            doors.UpdateTargetTemperature(device.NodeID, ts.TemperatureSetpoint);
                        }
                    }
                    else
                    {
                        /* Heizung direkt ansteuern */
                        bool isCompleteOff = ts.TemperatureSetpoint < 10;

                        if (isCompleteOff)
                        {
                            await device.SetTemperature(ts.TemperatureSetpoint);
                            await device.SetLevel(0);
                        }
                        else
                        {
                            /* Im Sommer einfach nicht einschalten, nur aus */
                            if (!TemperatureSchedule.IsSummertimeOffDate(DateTime.Today))
                            {
                                /* Wenn es draußen zu warm ist, dann nicht einschalten */
                                if (!TemperatureSchedule.IsTooHot(weather))
                                {
                                    if (heating.SpecificType == KnownDeviceTypes.FibaroHeatController)
                                    {
                                        await device.SetLevel(99);
                                    }
                                    await device.SetTemperature(ts.TemperatureSetpoint);
                                }
                                else
                                {
                                    Logger.Instance.LogInfo("Draußen zu warm, Heizung wird nicht eingeschaltet");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogDebug("Fehler beim Ausführen einer Temperaturplanregel" + ex.Message);
                }
            });
            return action;
        }

        /// <summary>
        /// Konfiguriert die Wiederholungs- und Timerdaten für die angegebenen Daten
        /// </summary>
        /// <param name="ts">TemperatureSchedule</param>
        /// <returns>TimerRecurrence</returns>
        private static TimerRecurrence CreateRecurrence(TemperatureSchedule ts)
        {
            TimerRecurrence recurrence = new TimerRecurrence();
            recurrence.Hour = ts.Hour;
            recurrence.Minute = ts.Minute;
            recurrence.Weekdays = ts.Weekday;
            return recurrence;
        }



        /// <summary>
        /// Konfiguriert die Wiederholungs- und Timerdaten für die angegebenen Daten
        /// </summary>
        /// <param name="ts">AlarmSchedule</param>
        /// <returns>TimerRecurrence</returns>
        private static TimerRecurrence CreateRecurrence(AlarmSchedule item)
        {
            if (!item.HasRecurrence || item.Recurrence == null)
            {
                throw new InvalidOperationException("Der übergeben Termin hat keine Terminserie und kann daher nicht als Wiederholung angelegt werden.");
            }

            TimerRecurrence recurrence = new TimerRecurrence();
            recurrence.Hour = item.AlarmTime.Hour;
            recurrence.Minute = item.AlarmTime.Minute;
            recurrence.Weekdays = item.Recurrence.Weekdays;
            recurrence.Interval = item.Recurrence.Interval;
            recurrence.From = item.Recurrence.From ?? item.AlarmTime;
            recurrence.Until = item.Recurrence.Until;
            return recurrence;
        }


        /// <summary>
        /// Löst das Changed-Ereignis aus
        /// </summary>
        public async Task RaiseChanged()
        {
            /* Termine je nach aktuellem Ferienzustand an/aus schaltlen */
            await ApplyFerien();

            /* Termine neu aus DB laden */
            this.Rules =
                CreateTemperatureScheduleRulesFromDb(_db, _devices, _doors, _weather)
                .Concat(CreateAlarmScheduleRulesFromDb(_db, _events))
                .ToList()
                .AsReadOnly();

            /* Alle Zuhörern (i.A. RuleEngine) bescheid geben, dass wir uns verändert haben */
            this.Changed?.Invoke(this, EventArgs.Empty);
        }





        /// <summary>
        /// Selbsttestfunktion ausführen (ICanSelfTest)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if (this.Rules == null || !this.Rules.Any())
            {
                yield return new SelfTestResult(true, "Regelverwaltung", "Keine Regeln geladen");
            }
            if (this.Changed == null)
            {
                yield return new SelfTestResult(true, "Regelverwaltung", "Niemand hört auf das Changed Ereignis");
            }
        }

    }
}
