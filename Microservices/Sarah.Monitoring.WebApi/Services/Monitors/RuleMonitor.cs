using System.Collections.ObjectModel;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;
using Microsoft.Extensions.Logging;
// using Sarah.Rules.Actions;
// using Sarah.Rules.Conditions;

namespace Sarah.Monitoring.Monitors
{
    /// <summary>
    /// Verbindet die Regeln aus der Datenbank mit der eigentlichen RuleEngine und
    /// aktualisiert diese, wenn sich die Datenbankeinträge ändern
    /// </summary>
    public class RuleMonitor : IRuleStore, ICanSelfTest, IMonitor
    {
        private readonly IRuleService _ruleService;
        private readonly IFerienInfoProvider _ferien;
        private readonly IEventProcessingService _events;
        private readonly IDBService _db;
        private readonly IDeviceService _devices;
        private readonly IDoorMonitor _doors;
        private readonly IWeatherProvider _weather;
        private readonly ILogger<RuleMonitor> _logger;

        public RuleMonitor(IRuleService ruleService, IFerienInfoProvider ferien, IEventProcessingService events, IDBService db, IDeviceService devices, IDoorMonitor doors, IWeatherProvider weather, ILogger<RuleMonitor> logger)
        {
            _ruleService = ruleService;
            _ferien = ferien;
            _events = events;
            _db = db;
            _devices = devices;
            _doors = doors;
            _weather = weather;
            _logger = logger;
        }

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
                    // Rules depend on Sarah.Rules project which doesn't exist
                    // _rules = CreateTemperatureScheduleRulesFromDb(_db, _devices, _doors, _weather)
                    //     .Concat(CreateAlarmScheduleRulesFromDb(_db, _events))
                    //     .ToList()
                    //     .AsReadOnly();
                    _rules = new List<Rule>().AsReadOnly();
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
            _logger.LogDebug("RuleMonitor gestartet.");


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
                _logger.LogError("Fehler beim deaktiveren alter Erinnerungen", ex);
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
                _logger.LogError("Fehler beim deaktiveren alter Erinnerungen", ex);
            }
        }



        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            return new List<SelfTestResult>();
        }

        public Task RaiseChanged()
        {
            // TODO: Implement when Sarah.Rules project is available
            return Task.CompletedTask;
        }
    }
}
