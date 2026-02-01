using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Rules.Actions;
using Sarah.Rules.Conditions;
using Microsoft.Extensions.Logging;
using Sarah.Messaging.RabbitMQ;

namespace Sarah.Rules
{
    /// <summary>
    /// Regelspeicher für fest gecodede Regeln
    /// </summary>
    public class HardCodedRuleStore : IRuleStore
    {
        private readonly RabbitMQClient _rabbitMQ;
        private readonly IDeviceService _devices;
        private readonly IPersonService _persons;
        private readonly IEmailNotifier _emails;
        private readonly IFerienInfoProvider _ferien;
        private readonly IDeviceService _deviceServiceClient;
        private List<Rule> _rules = new List<Rule>();
        private readonly IWeatherProvider _weather;

        private readonly ILogger<HardCodedRuleStore> _logger;


        /// <summary>
        /// Die Regeln dieses Speichers.
        /// </summary>
        public IReadOnlyCollection<Rule> Rules => _rules.AsReadOnly();

        /// <summary>
        ///
        /// </summary>
        public event EventHandler? Changed; // wird momentan nie ausgelöst, da die Regeln immer fest im ctor erzeugt werden



        /// <summary>
        /// ctor
        /// </summary>
        public HardCodedRuleStore(IWeatherProvider weather, RabbitMQClient rabbitMQ, IDeviceService devices, IPersonService persons, IEmailNotifier email, IFerienInfoProvider ferien, IDeviceService deviceServiceClient, ILogger<HardCodedRuleStore> logger)
        {
            this._logger = logger;
            this._rabbitMQ = rabbitMQ;
            this._devices = devices;
            this._weather = weather;
            this._persons = persons;
            this._emails = email;
            this._ferien = ferien;
            this._deviceServiceClient = deviceServiceClient;
            this.CreateRules();
        }








        /// <summary>
        /// Erstellt die List mit fest im Code hinterlegten Regeln, so lange es noch keine UI dafür gibt...
        /// </summary>
        private void CreateRules()
        {
            this._rules = new List<Rule>();

            AddDeviceRules();

            AddChrisStundenplanRules();

            AddGeoFenceRules();

            AddTrackerButtonRules();

            //AddLukasStundenplanRules();

            //AddLukasKloRules();

            //AddLukasGehInsBettRules();

            this.Changed?.Invoke(this, EventArgs.Empty);
           
        }

        private void AddTrackerButtonRules()
        {
            this._rules.Add(new Rule()
            {
                Condition = new TrackerButtonPressedCondition(247, _devices),
                Action = new SayAction("Warnung: Lukas hat den SOS Knopf seines Trackers gedrückt.", _rabbitMQ),
                Name = "Sprachausgabe, wenn Button von SenseCap Tracker 247 gedrückt wurde"
            });
            this._rules.Add(new Rule()
            {
                Condition = new TrackerButtonPressedCondition(246, _devices),
                Action = new SayAction("Warnung: Christian hat den SOS Knopf seines Trackers gedrückt.", _rabbitMQ),
                Name = "Sprachausgabe, wenn Button von SenseCap Tracker 246 gedrückt wurde"
            });
            this._rules.Add(new Rule()
            {
                Condition = new TrackerButtonPressedCondition(245, _devices),
                Action = new SayAction("Warnung: Hannah hat den SOSKnopf ihres Trackers gedrückt.", _rabbitMQ),
                Name = "Sprachausgabe, wenn Button von SenseCap Tracker 245 gedrückt wurde"
            });
        }

        private void AddDeviceRules()
        {
            //this._rules.Add(new Rule()
            //{
            //    Condition = new ButtonPressedCondition(12),
            //    Action = new ToggleLampAction(21),
            //    Name = "RedButton schaltet Lampe 21"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new ButtonPressedCondition(12),
            //    Action = new ToggleLampAction(30),
            //    Name = "RedButton schaltet Lampe LED Strip 30"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new CombinedCondition(11, ConditionOperator.OR,
            //    new ButtonPressedCondition(11, 1)
            //    /*  new ButtonPressedCondition(11, 2)*/
            //    /*,new ButtonPressedCondition(11, 3), new ButtonPressedCondition(11, 4),
            //    new ButtonPressedCondition(11, 5), new ButtonPressedCondition(11, 6) */
            //    ),
            //    Action = new ToggleLampAction(30),
            //    Name = "Wandschalter 11 schaltet LED 30 an/aus"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new ButtonPressedCondition(11,2),
            //    Action = new ToggleLampAction(21),
            //    Name = "Wandschalter 11 schaltet Lampe 21"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new ButtonPressedCondition(11, 5),
            //    Action = new SetLampColorAndBrightnessAction(30, 255, "#FFBC1F"),
            //    Name = "Wandschalter 11 schaltet Licht LED Strip 30 auf Warmweiss"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new ButtonPressedCondition(11, 6),
            //    Action = new SetLampColorAndBrightnessAction(30, 255, "#FFFFFF"),
            //    Name = "Wandschalter 11 schaltet Licht Strip 30 auf Kaltweiss"
            //});

            this._rules.Add(new Rule()
            {
                Condition = new ButtonPressedCondition(41, 1, _devices),
                Action = new ToggleLampAction(30, _devices),
                Name = "Keyfob41 Schalter 1 schaltet LED 30 an/aus"
            });
            this._rules.Add(new Rule()
            {
                Condition = new ButtonPressedCondition(41, 2, _devices),
                Action = new ToggleLampAction(21, _devices),
                Name = "Keyfob41 Schalter 2 schaltet Lampe 21 an/aus"
            });

            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(2, ConditionOperator.AND,
                    new NoOnePresentCondition(_persons),
                    new DoorSensorCondition(2, _devices) { Value = DoorSensorState.Offen }),
                Action = new CombinedAction(
                        new SendMailAction("c.fonden@die-rooter.de", "Tür Arbeitszimmer offen", "Die Türe im Arbeitszimmer wurde geöffnet, obwohl keine bekannte Person daheim ist", _emails, _logger),
                        new SayAction("Die Türe im Arbeitszimmer ist offen, obwohl keine bekannte Person daheim ist.", _rabbitMQ)
                    ),
                Name = "Email an c.fonden@die-rooter.de wenn Terassentür im Arbeitszimmer offen und keiner zu Hause"
            });
            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(34, ConditionOperator.AND,
                    new NoOnePresentCondition(_persons),
                    new DoorSensorCondition(34, _devices) { Value = DoorSensorState.Offen }),
                Action = new CombinedAction(
                        new SendMailAction("c.fonden@die-rooter.de;h.fonden@die-rooter.de", "Tür Haustüre offen", "Die Haustüre wurde geöffnet, obwohl keine bekannte Person daheim ist", _emails, _logger),
                        new SayAction("Die die Haustüre ist offen, obwohl keine bekannte Person daheim ist. Alarm wird ausgelöst und Kamer wird aktiviert. Bilder werden an die Cloud übertragen.", _rabbitMQ, Sarah.Messaging.RabbitMQ.Messages.SpeechVolume.VeryLoud), 
                        new StartAudioAction("alert1.wav", "", _rabbitMQ),
                        new StartSceneAction("RedAlert", _deviceServiceClient)
                    ),
                Name = "Roter Alarm und Email an c.fonden@die-rooter.de;h.fonden@die-rooter.de wenn die Haustüre offen und keiner zu Hause ist"
            });


            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(34, ConditionOperator.AND,
                    new SomeOnePresentCondition(_persons),
                    new DoorSensorCondition(34, _devices) { Value = DoorSensorState.Geschlossen }),
                Action =  new CombinedAction(
                    new StopSceneAction("RedAlert", _deviceServiceClient),
                    new StopAudioAction("", _rabbitMQ)
                ),
                Name = "Roten Alarm anhalten wenn Haustüre geschlossen"
            });


            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(13, ConditionOperator.AND,
                    new PresenceCondition(13, true, _devices),
                    new LuminanceSmallerThanCondition(13, 40, _devices)
                ),
                Action = new SetLampColorAndBrightnessAction(14, 255, "#FFBC1F", _devices, _logger),
                Name = "Lampe 14 an wenn jemand im Arbeitszimmer (MultiSensor 13) ist."
            });


            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(13, ConditionOperator.AND,
                    new PredicateCondition(13, id => DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 10),
                    new PresenceCondition(13, true, _devices)),
                Action = new SayOnceAction(CreateGreetingStringExpr("Christian"), "speaker1", _rabbitMQ, TimeSpan.FromHours(23)),
                Name = "Morgens Guten morgen sagen wenn jemand im Arbeitszimmer (MultiSensor 13) ist."
            });



            //this._rules.Add(new Rule()
            //{
            //    Condition = new CombinedCondition(25, ConditionOperator.AND,
            //        new PredicateCondition(25, id => DateTime.Now.Hour >= 6 && DateTime.Now.Hour < 21),
            //        new WallPlugPowerIncreasingCondition(25)),
            //    Action = new SayOnceAction(CreateGreetingStringExpr("Hannah", false), "speaker3", TimeSpan.FromHours(23)),
            //    Name = "Hannah begrüßen sagen wenn der Fernseher angeht."
            //});

            //this._rules.Add(new Rule()
            //{
            //    Condition = new CombinedCondition(25, ConditionOperator.AND,
            //        new PredicateCondition(25, id => DateTime.Now.Hour >= 19),
            //        new WallPlugPowerDecreasingCondition(25)),
            //    Action = new CombinedAction(
            //        //new SetLampColorAndBrightnessAction(24, 0, null),
            //        new SayOnceAction(CreateGreetingStringExpr("alle Miteinander", false), "speaker3", TimeSpan.FromHours(23))
            //        ),
            //    Name = "Gute Nacht sagen wenn abends der Fernseher aus geht."
            //});


            this._rules.Add(new Rule()
            {
                Condition = new PresenceCondition(13, false, _devices),
                Action = new SetLampColorAndBrightnessAction(14, 0, "#000000", _devices, _logger),
                Name = "Lampe 14 aus wenn niemand im Arbeitszimmer (MultiSensor 13) ist."
            });

            this._rules.Add(new Rule()
            {
                Condition = new WallPlugPowerOffCondition(251, _devices),
                Action =  new SayAction("Der Wäschetrockner ist fertig.", _rabbitMQ),
                Name = "Sprachausgabe, wenn Leistung an Node 251 (Trockner) abfällt"
            });
            this._rules.Add(new Rule()
            {
                Condition = new WallPlugPowerOffCondition(252, _devices),
                Action = new SayAction("Die Waschmaschine ist fertig.", _rabbitMQ),
                Name = "Sprachausgabe, wenn Leistung an Node 252 (Waschmaschine) abfällt"
            });
            //this._rules.Add(new Rule()
            //{
            //    Condition = new CombinedCondition(25, ConditionOperator.OR,
            //        new WallPlugSwitchOffCondition(25),
            //        new WallPlugPowerOffCondition(25)
            //        ),
            //    Action = new CombinedAction(
            //        new SendMailAction("c.fonden@die-rooter.de", "Fernseher ist aus", "Die Steckdose des Fernsehers (25) meldet Zustand \"aus\"!"),
            //        new SayAction("Achtung! Die Steckdose des Fernsehers (Gerät 25) ist aus!", NotificationEngine.BroadcastAllSpeakers)
            //        ),
            //    Name = "Warnung wenn Steckdose Fernseher (25) aus ist"
            //});
            this._rules.Add(new Rule()
            {
                Condition = new WallPlugPowerOffCondition(20, _devices),
                Action = new CombinedAction(
                    //new SendMailAction("c.fonden@die-rooter.de", "☕ Kaffee ist fertig", "Die Steckdose der Kaffeemaschine meldet, dass die Leistung abgefallen ist."),
                    new WallPlugOffAction(20, _devices),
                    new SayAction("Der Kaffee ist fertig.", _rabbitMQ)
                    ),
                Name = "Kaffeemaschine: Sprachausgabe und Node 20 ausschalten, wenn Leistung an Node 20 abfällt"
            });

            //this._rules.Add(new Rule()
            //{
            //    Condition = new CombinedCondition(27, ConditionOperator.OR,
            //        new WallPlugSwitchOffCondition(27),
            //        new WallPlugPowerOffCondition(27)
            //        ),
            //    Action = new CombinedAction(
            //        new SendMailAction("c.fonden@die-rooter.de", "Fernseher ist aus", "Die Steckdose der Festplatten (27) meldet Zustand \"aus\"!"),
            //        new SayAction("Achtung! Die Steckdose der Festplatten (Gerät 27) ist aus!", NotificationEngine.BroadcastAllSpeakers)
            //        ),
            //    Name = "Warnung wenn Steckdose Festplatten (27) aus ist"
            //});

            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(0, ConditionOperator.AND,
                        new PredicateCondition(0, id => DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 11),
                        new PersonPresenceChangedCondition("Christian", true)),
                Action = new CombinedAction(
                    new WallPlugOnAction(20, _devices),
                    new SayAction("Hallo Christian, ich schalte die Kaffeemaschine ein.", _rabbitMQ)
                    ),
                Name = "Kaffeemaschine (Steckdose 20) einschalten wenn Christian nach Hause kommt (zwischen 7 und 11 Uhr)"
            });

            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(0, ConditionOperator.AND,
                    new AirQualityCondition(31),  // Stinksensor arbeitszimmer
                    new PresenceCondition(13, true, _devices) // jemand anwesend im Arbeitszimmer
                    ),
                Action = new SayAirQualityAction(31, "", _rabbitMQ),
                Name = "Luftqualität Stinksensor Sprachausgabe Arbeitszimmer (nur dort)"
            }); ;

            this._rules.Add(new Rule()
            {
                Condition = new AlertCondition(38, _devices),
                Action = new CombinedAction
                (
                    new SendMailAction("c.fonden@die-rooter.de", "🧯 Feueralarm", "Rauchmelder 38 meldet Feueralarm!!!", _emails, _logger),
                    new SendMailAction("h.fonden@die-rooter.de", "🧯 Feueralarm", "Rauchmelder 38 meldet Feueralarm!", _emails, _logger),
                    new SayAction("Achtung, Rauchmelder 38 meldet Feueralarm!", _rabbitMQ)
                ),
                Name = "E-Mail bei Feueralarm Node 38"
            });
        }

        private void AddGeoFenceRules()
        {
            this._rules.Add(new Rule()
            {
                Condition = new GeoFenceChangedCondition(),
                Action = new ActionRuleAction((NetworkEvent e) =>
                {
                    PersonGeoFenceEvent evt = (PersonGeoFenceEvent) e;
                    string text;
                    if(evt.CurrentGeoFence != null)
                    {
                        text = $"{evt.PersonName} hat {evt.CurrentGeoFence} erreicht";
                    } 
                    else if (evt.PreviousGeoFence != null)
                    {
                        text = $"{evt.PersonName} hat {evt.PreviousGeoFence} verlassen";
                    } 
                    else
                    {
                        text = $"{evt.PersonName} ist nun unterwegs";
                    }
                    _rabbitMQ.PublishAsync(new Sarah.Messaging.RabbitMQ.Messages.SayMessage(text)).Wait();
                }, _logger),
                Name = "Sprachausgabe beim betreten oder verlassen eines GeoFence-Bereichs"
            });
        }

        private Func<string> CreateGreetingStringExpr(string personName)
        {
            return () =>
            {
                string greet = Greeting.Current + ", " + personName + ". Es ist " + DateTime.Now.Hour + " Uhr " + DateTime.Now.Minute + ". ";

                string weatherInfo = _weather.GetCurrentWeatherString();
                if (!String.IsNullOrWhiteSpace(weatherInfo))
                {
                    greet += ". " + weatherInfo;
                }

                string weatherForecast = _weather.GetWeatherForecastStringForToday();
                if (!String.IsNullOrWhiteSpace(weatherForecast))
                {
                    greet += ". " + weatherForecast;
                }

                /* Ferien morgen zu Ende */
                string? aktuelleFerien = _ferien.AktuelleFerien?.Name;
                if (!String.IsNullOrWhiteSpace(aktuelleFerien) && _ferien.AktuelleFerien?.Ende == DateTime.Today)
                {
                    greet += ". " + "Heute sind die " + aktuelleFerien + " zu Ende.";
                }

                /* Ferien beginnen morgen */
                if (_ferien.Ferien.Any(item => item.Start == DateTime.Today.AddDays(1)))
                {
                    greet += ". " + "Ab morgen sind Ferien. ";
                }

                // if (appendDeseaseStats)
                // {
                //     string deaseasesInfo = NotificationEngine.Instance.Deseases?.GetLocalStatsText();
                //     if (!String.IsNullOrWhiteSpace(deaseasesInfo))
                //     {
                //         greet += ". " + deaseasesInfo;
                //     }
                // }
                return greet;
            };
        }

        private void AddChrisStundenplanRules()
        {
            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 7, Minute = 15, Weekdays = Weekdays.Montag|Weekdays.Dienstag|Weekdays.Mittwoch|Weekdays.Donnerstag|Weekdays.Freitag }),
            //    Action = 
            //        new ActionRuleAction(() => 
            //        {
            //            try
            //            {
            //                string from = "Kastanienallee 25, 71638 Ludwigsburg";
            //                string to = "Osterholzallee 144, 71636 Ludwigsburg";

            //                var route = NotificationEngine.Instance.Geo.GetRouteInfo(from, to).Result;

            //                string text = "Die Fahrtzeit ins Büro beträgt zur Zeit "
            //                    + Math.Round(route.Duration.TotalMinutes,0) + " Minuten. "
            //                    + route.TrafficCongestion;

            //                NotificationEngine.Instance.Voice.Say( text );
            //            }
            //            catch (Exception ex)
            //            {
            //                _logger.LogError(ex, "Error in traffic sensors");
            //                NotificationEngine.Instance.Voice.Say("Meine Verkehrssensoren sind leider gestört. ");
            //            }
            //        }
            //        ),
            //    Name = "Ansage Pendelzeit 7:15 Uhr"
            //});

            this._rules.Add(new Rule()
            {
                Condition = new TimerCondition(new TimerRecurrence() { Hour = 6, Minute = 45, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag }),
                Action =  new SetLampColorAndBrightnessAction(24, 0, "#FFFFFF", _devices, _logger),
                Name = "Lampe Wohnzimmer um 6:45 Uhr aus machen"
            });

            this._rules.Add(new Rule()
            {
                Condition = new TimerCondition(new TimerRecurrence() { Hour = 12, Minute = 0, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag }),
                Action = new CombinedAction(
                    new SetLampColorAndBrightnessAction(14, 255, "#FF0000", _devices, _logger),
                    new SayAction("Es ist Zeit für die Mittagspause.", "speaker1", _rabbitMQ)
                    ),
                Name = "Erinnerung an Mittagspause"
            });



            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(0, ConditionOperator.AND,
                    new TimerCondition(new TimerRecurrence() { Hour = 14, Minute = 0, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag }),
                    new PresenceCondition(13, true, _devices) // jemand anwesend im Arbeitszimmer
                    ),
                Action = new CombinedAction(
                    new SetLampColorAndBrightnessAction(14, 0, "#FFFFFF", _devices, _logger),
                    new SayAction("Es ist Zeit zum weiter arbeiten", "speaker1", _rabbitMQ)
                    ),
                Name = "Erinnerung an Ende der Mittagspause"
            });

            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(0, ConditionOperator.AND,
                    new TimerCondition(new TimerRecurrence() { Hour = 8, Minute = 0, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag }),
                    new PresenceCondition(13, true, _devices) // jemand anwesend im Arbeitszimmer
                    ),
                Action = new CombinedAction(
                    new SetLampColorAndBrightnessAction(14, 255, "#0000FF", _devices, _logger),
                    new SayAction("Es ist Zeit fürs Daily", "speaker1", _rabbitMQ)
                    ),
                Name = "Erinnerung Daily 8:00"
            });

            this._rules.Add(new Rule()
            {
                Condition = new CombinedCondition(0, ConditionOperator.AND,
                    new TimerCondition(new TimerRecurrence() { Hour = 8, Minute = 20, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag }),
                    new PresenceCondition(13, true, _devices) // jemand anwesend im Arbeitszimmer
                    ),
                Action = new CombinedAction(
                    new SetLampColorAndBrightnessAction(14, 255, "00FF00", _devices, _logger),
                    new SayAction("Das Daily sollte nun zu Ende sein!", "speaker1", _rabbitMQ)
                    ),
                Name = "Erinnerung Daily 8:20"
            });

            this._rules.Add(new Rule()
            {
                Condition = new TimerCondition(new TimerRecurrence() { Hour = 8, Minute = 21, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag }),
                Action = new SetLampColorAndBrightnessAction(14, 0, "FFFFFF", _devices, _logger),
                Name = "Erinnerung Daily 8:21->Lampe aus"
            });
        }


        private void AddLukasStundenplanRules()
        {
            /* Corona 2021-Homeschooling Erinnerung */
            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 8, Minute = 15, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag }),
            //    Action = new SayAction("Lukas muss in 15 Minuten mit der Schule beginnen.", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas Homeschooling"
            //});


            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 7, Minute = 30, Weekdays = Weekdays.Montag }),
            //    Action = new SayAction("Lukas muss los zur Schule!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas muss los zur Schule (Montag)"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 8, Minute = 15, Weekdays = Weekdays.Dienstag }),
            //    Action = new SayAction("Lukas muss los zur Schule!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas muss los zur Schule (Dienstag)"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 7, Minute = 30, Weekdays = Weekdays.Mittwoch }),
            //    Action = new SayAction("Lukas muss los zur Schule!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas muss los zur Schule (Mittwoch)"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 8, Minute = 15, Weekdays = Weekdays.Donnerstag }),
            //    Action = new SayAction("Lukas muss los zur Schule!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas muss los zur Schule (Donnerstag)"
            //});
            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 7, Minute = 30, Weekdays = Weekdays.Freitag }),
            //    Action = new SayAction("Lukas muss los zur Schule!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas muss los zur Schule (Freitag)"
            //});



            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 12, Minute = 05, Weekdays = Weekdays.Montag }),
            //    Action = new SayAction("Lukas von der Schule abholen!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas von der Schule abholen (Montag)"
            //});

            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 12, Minute = 5, Weekdays = Weekdays.Dienstag }),
            //    Action = new SayAction("Lukas von der Schule abholen!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas von der Schule abholen (Dienstag)"
            //});

            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 12, Minute = 50, Weekdays = Weekdays.Mittwoch }),
            //    Action = new SayAction("Lukas von der Schule abholen!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas von der Schule abholen (Mittwoch)"
            //});

            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 12, Minute = 05, Weekdays = Weekdays.Donnerstag }),
            //    Action = new SayAction("Lukas von der Schule abholen!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas von der Schule abholen (Donnerstag)"
            //});

            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 13, Minute = 25, Weekdays = Weekdays.Donnerstag }),
            //    Action = new SayAction("Lukas muss zum Nachmittagsunterricht!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas Nachmittags Unterricht (Donnerstag)"
            //});

            //this._rules.Add(new Rule()
            //{
            //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 12, Minute = 50, Weekdays = Weekdays.Freitag }),
            //    Action = new SayAction("Lukas von der Schule abholen!", NotificationEngine.BroadcastAllSpeakers),
            //    Name = "Erinnerung Lukas von der Schule abholen (Freitag)"
            //});
        }

        //private void AddLukasKloRules()
        //{
        //    //this._rules.Add(new Rule()
        //    //{
        //    //    Condition = new TimerCondition(new TimerRecurrence() { Hour = 18, Minute = 0, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag | Weekdays.Samstag | Weekdays.Sonntag }),
        //    //    Action = new CombinedAction(
        //    //        new BlinkAction(24, 1),
        //    //        new SayAction("Lukas, geh bitte aufs Klo. ", "speaker2")
        //    //        ),
        //    //    Name = "Lukas aufs Klo 18:00"
        //    //});

        //    this._rules.Add(new Rule()
        //    {
        //        Condition = new TimerCondition(new TimerRecurrence() { Hour = 18, Minute = 30, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag | Weekdays.Samstag | Weekdays.Sonntag }),
        //        Action = new CombinedAction(
        //            //new BlinkAction(24, 1),
        //            new SayAction("Lukas sollte jetzt aufs Klo gehen. ", "speaker2")
        //            ),
        //        Name = "Lukas aufs Klo 18:30"
        //    });

        //    this._rules.Add(new Rule()
        //    {
        //        Condition = new TimerCondition(new TimerRecurrence() { Hour = 19, Minute = 00, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag | Weekdays.Samstag | Weekdays.Sonntag }),
        //        Action = new CombinedAction(
        //            //new BlinkAction(24, 1),
        //            new SayAction("Lukas, geh bitte auf die Toilette! ", "speaker2")
        //            ),
        //        Name = "Lukas aufs Klo 19:00"
        //    });

        //    this._rules.Add(new Rule()
        //    {
        //        Condition = new TimerCondition(new TimerRecurrence() { Hour = 19, Minute = 30, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag | Weekdays.Samstag | Weekdays.Sonntag }),
        //        Action = new CombinedAction(
        //            //new BlinkAction(24, 1),
        //            new SayAction("Es ist Zeit für Lukas, aufs Klo zu gehen. ", "speaker2")
        //            ),
        //        Name = "Lukas aufs Klo 19:00"
        //    });

        //    this._rules.Add(new Rule()
        //    {
        //        Condition = new TimerCondition(new TimerRecurrence() { Hour = 20, Minute = 00, Weekdays = Weekdays.Montag | Weekdays.Dienstag | Weekdays.Mittwoch | Weekdays.Donnerstag | Weekdays.Freitag | Weekdays.Samstag | Weekdays.Sonntag }),
        //        Action = new CombinedAction(
        //            //new BlinkAction(24, 1),
        //            new SayAction("Lukas sollte nun nochmal aufs Klo gehen. ", "speaker2")
        //            ),
        //        Name = "Lukas aufs Klo 20:00"
        //    });

        //}
    }
}
