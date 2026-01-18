using Sarah.API.BusinessObjects;
using Sarah.API.Extensions;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Rules.Conditions;
using System.Text;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules
{
    /// <summary>
    /// Die RuleEngine (Singleton)
    /// </summary>
    public sealed class RuleService : IRuleService, INetworkEventSubscriber, IDisposable
    {
        private readonly IDeviceService _devices;
        private readonly RabbitMQClient _rabbitMQ;



        /// <summary>
        /// Die einzelnen Regeldefinitionen
        /// </summary>
        public IEnumerable<Rule> Rules => this.RuleStores.SelectMany(store => store.Rules);

        /// <summary>
        /// Internes Logging für die Ruleengine
        /// </summary>
        public StringBuilder Log { get; } = new StringBuilder();

        /// <summary>
        /// Verwaltung für Zeitgesteuerte Ereignisse
        /// </summary>
        private TimerEngine Timers { get; } 

        private readonly ILogger _logger;

        /// <summary>
        /// Alle Regel-Quellen
        /// </summary>
        private List<IRuleStore> RuleStores { get; } = new List<IRuleStore>();

        public RuleService(IDeviceService devices, RabbitMQClient rabbitMQ, ILogger logger) 
        { 
            this._devices = devices;
            this._rabbitMQ = rabbitMQ;
            this.Timers = new TimerEngine(rabbitMQ, logger);
            this._logger = logger;
        }

        private object _evaluateRulesLock = new object();

        /// <summary>
        /// Evaluiert alle Regeln führt bei zutreffen die verbundene Aktion aus
        /// </summary>
        public void EvaluateRules(NetworkEvent e)
        {
            lock (_evaluateRulesLock)
            {
                foreach (var rule in Rules.Where(r => r.Condition.TargetNodeId == e.SourceNodeId || r.Condition.TargetNodeId == 0))
                {
                    try
                    {
                        bool hasOccuredLately = rule.LastOccurence.HasValue && (DateTime.Now - rule.LastOccurence.Value).TotalSeconds < 5;
                        if (!hasOccuredLately && rule.Condition.Evaluate(e))
                        {
                            AddLog("Regel " + rule.Name + " aktiviert");
                            rule.Action.Execute(e);
                            rule.LastOccurence = DateTime.Now;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("RuleEngine: Error while evaluating rule {RuleName}: {ErrorMessage}", rule.Name, ex.Message);
                        _logger.LogDebug("StackTrace: {StackTrace}", ex.StackTrace);
                    }
                }
            }
        }

        private void AddLog(string msg)
        {
            if (Log.Length > 10000)
            {
                Log.Clear();
            }
            Log.Insert(0, DateTime.Now + "\t" + msg + Environment.NewLine); //Neuestes oben
            _logger.LogInformation("{Message}", msg);

            // Man muss Dinge auch aussprechen dürfen!
            //Notifications.NotificationEngine.Instance.Voice?.Say(msg);
        }



        /// <summary>
        /// Registriert eine neue Regeldatenquelle für die Ruleengine
        /// </summary>
        /// <param name="storage"></param>
        public void RegisterRuleStore(IRuleStore storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }
            lock (this.RuleStores)
            {
                if (!this.RuleStores.Contains(storage))
                {
                    this.RuleStores.Add(storage);
                    this.UpdateTimerRules();
                    storage.Changed += (sender, e) => UpdateTimerRules();
                }
            }
        }

        /// <summary>
        /// Startet das Regelwerk unter beachtung der im Store vorhandenen Regeln
        /// </summary>
        /// <param name="store"></param>
        private void UpdateTimerRules()
        {
            this.Timers.Clear();
            foreach (var rule in Rules)
            {
                TimerCondition timerCondition = rule.Condition as TimerCondition;
                if (timerCondition != null)
                {
                    if (timerCondition.IsOneShot)
                    {
                        if (timerCondition.DateTime.IsInFuture()) //nur Timer aktivieren, die mindestens 5 sekunden in der Zukunft liegen
                        {
                            this.Timers.Register(timerCondition.DateTime);
                            _logger.LogDebug("RecurrenceTimer for {RuleName} ticks at {TickTime} (one shot)", rule.Name, timerCondition.DateTime);
                        }
                    }
                    else
                    {
                        this.Timers.Register(timerCondition.Recurrence);
                        _logger.LogDebug("RecurrenceTimer for {RuleName} ticks at {NextTick} (recurring)", rule.Name, timerCondition.Recurrence.GetNext());

                    }
                }
            }
        }

        /// <summary>
        /// Notify-Methode des EventAggregators
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Task Notify(NetworkEvent e)
        {
            return Task.Run(() => this.EvaluateRules(e));
        }

        /// <summary>
        /// From IDisposable
        /// </summary>
        public void Dispose()
        {
            this.Timers?.Dispose();
        }


        /// <summary>
        /// Verwaltung der Timer-Events für die einzelnen TimerConditions
        /// </summary>
        private sealed class TimerEngine : IDisposable
        {
            /// <summary>
            /// Alle registrierten Timer-Ereignisse
            /// </summary>
            private List<RecurrenceTimer> RegisteredRecurrences { get; } = new List<RecurrenceTimer>();
            private readonly ILogger _logger;
            private readonly RabbitMQClient _rabbitMQ;

            /// <summary>
            ///
            /// </summary>
            public TimerEngine(RabbitMQClient rabbitMQ, ILogger logger)
            {
                this._logger = logger;
                this._rabbitMQ = rabbitMQ;
                _RecreateTimersTimer = new Timer(RecreateTimer_Tick, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));
            }


            /// <summary>
            /// Timer, der jeden Tag überprüft, ob es noch Termine ohne Timer gibt weil diese bisher zu weit in der Zukunft lagen
            /// </summary>
            private readonly Timer _RecreateTimersTimer;

            private void RecreateTimer_Tick(object state)
            {
                foreach (RecurrenceTimer item in this.RegisteredRecurrences.Where(timer => !timer.IsTimerCreated).ToList())
                {
                    item.CreateTimer();
                }
            }


            /// <summary>
            /// Löscht alle Timer-Einträge
            /// </summary>
            public void Clear()
            {
                RegisteredRecurrences.ForEach(timer => timer.Dispose());
                this.RegisteredRecurrences.Clear();
            }

            /// <summary>
            /// Registert eine neue Timer-Wiederholung bei der Engine
            /// </summary>
            /// <param name="recurrence"></param>
            internal void Register(TimerRecurrence recurrence)
            {
                if (recurrence.Until.HasValue && !recurrence.Until.Value.IsInFuture())
                {
                    _logger.LogDebug("recurrence.until ist in Vergangenheit -> übersprungen");
                }
                else
                {
                    RecurrenceTimer rt = new RecurrenceTimer(recurrence, _logger);
                    rt.Tick += recurrence_elapsed;
                    this.RegisteredRecurrences.Add(rt);
                }
            }

            internal void Register(DateTime occurance)
            {
                if (occurance.IsInFuture())
                {
                    RecurrenceTimer rt = new RecurrenceTimer(occurance, _logger);
                    rt.Tick += recurrence_elapsed;
                    this.RegisteredRecurrences.Add(rt);
                }
            }

            private async void recurrence_elapsed(object sender, EventArgs e)
            {
                var message = new TimerEventMessage(0);
                await _rabbitMQ.PublishAsync(message);
            }

            /// <summary>
            /// From IDisposable
            /// </summary>
            public void Dispose()
            {
                this._RecreateTimersTimer?.Dispose();
            }



            /// <summary>
            /// Kapselt die Zeitsteuerung für eine Recurrence
            /// </summary>
            private sealed class RecurrenceTimer : IDisposable
            {
                private TimerRecurrence RecurrenceDefinition { get; }
                private DateTime? OccuresOnceDate { get; }

                private Timer _timer;

                public bool IsTimerCreated => this._timer != null;
                private ILogger _logger;

                public RecurrenceTimer(TimerRecurrence recurrenceDefinition, ILogger logger)
                {
                    this.RecurrenceDefinition = recurrenceDefinition;
                    this.OccuresOnceDate = null;
                    this.CreateTimer();
                    this._logger = logger;
                }

                public RecurrenceTimer(DateTime occurance, ILogger logger)
                {
                    this.RecurrenceDefinition = null;
                    this.OccuresOnceDate = occurance;
                    this.CreateTimer();
                    this._logger = logger;
                }

                /// <summary>
                /// Erstellt einen Betriebssystemtimer für entweder einen festen Termin oder eine Terminserie.
                /// Falls das nächste Ereignis in der Vergangenheit oder zu weit in der Zukunft liegt,
                /// wird kein Betriebssystemtimer erstellt und IsTimerCreated bleibt false.
                /// Diese Methode muss dann später wieder aufgerufen werden, um den Timer zu erstellen.
                /// </summary>
                public void CreateTimer()
                {
                    if (this.RecurrenceDefinition != null)
                    {
                        DateTime nextOccurence = this.RecurrenceDefinition.GetNext();
                        this._timer = CreateOneShotTimer(nextOccurence);
                    }
                    else if (this.OccuresOnceDate.HasValue)
                    {
                        this._timer = CreateOneShotTimer(this.OccuresOnceDate.Value);
                    }
                    else
                    {
                        throw new InvalidOperationException("Weder Recurrence noch DateTime angegeben, kann keinen Timer für diesen Temrin erstellen");
                    }
                }

                private Timer CreateOneShotTimer(DateTime nextOccurence)
                {
                    TimeSpan dueTime = (nextOccurence - DateTime.Now);
                    if (dueTime.TotalSeconds > 0 && dueTime.TotalMilliseconds < (Int32.MaxValue - 2))
                    {
                        return new Timer(timer_tick_internal, null, dueTime, TimeSpan.FromMilliseconds(0));
                    }
                    else if (dueTime.TotalMilliseconds >= (Int32.MaxValue - 2))
                    {
                        _logger.LogWarning("Timer zu weit in der Zukunft -> wird nicht gestartet");
                        return null;
                    }
                    else
                    {
                        _logger.LogWarning("Timer läge in der Vergangenheit -> wird nicht gestartet");
                        return null;
                    }
                }

                private void timer_tick_internal(object state)
                {
                    this.Tick?.Invoke(this, EventArgs.Empty);
                    this._timer.Dispose();
                    if (this.RecurrenceDefinition != null)
                    {
                        DateTime next = this.RecurrenceDefinition.GetNext();
                        if (this.RecurrenceDefinition.Until.HasValue && this.RecurrenceDefinition.Until.Value < next)
                        {
                            _logger.LogDebug("Recurrence abgebrochen, da Unil in Vergangenheit liegt");
                        }
                        else
                        {
                            this._timer = CreateOneShotTimer(next);
                        }
                    } // else: OneShot Timer.
                }

                public void Dispose()
                {
                    this._timer?.Dispose();
                }

                public event EventHandler Tick;
            }

        }
    }
}
