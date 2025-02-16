using Sarah.API.Interfaces;
using Sarah.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Ein Pub/Sub Event Aggregator für Ereignisse im Netzwerk. 
    /// Alle Subscriber werden asynchron benachrichtigt.
    /// </summary>
    public class NetworkEventAggregator : IDisposable
    {
        #region Singleton Pattern
        private static NetworkEventAggregator _Instance = null;
        public static NetworkEventAggregator Instance
        {
            get
            {
                if(_Instance == null)
                {
                    _Instance = new NetworkEventAggregator();
                }
                return _Instance;
            }
        }

        public void Subscribe(INetworkEventSubscriber subscriber)
        {
            if(subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            this.Subscribers.Add(subscriber);
        }

        private NetworkEventAggregator()
        {
            Task unwawaitedTask = Task.Run(DoEventProcessing);
        }


        private bool ContinueProcessing { get; set; } = true;
        public void Dispose()
        {
            this.ContinueProcessing = false;
        }
        #endregion

        /// <summary>
        /// Events
        /// </summary>
        private ConcurrentQueue<NetworkEvent> Events { get; } = new ConcurrentQueue<NetworkEvent>();

        /// <summary>
        /// Anzahl Events im Puffer
        /// </summary>
        public int EventCount => Events.Count;

        /// <summary>
        /// Subscribers
        /// </summary>
        private ConcurrentBag<INetworkEventSubscriber> Subscribers { get; } = new ConcurrentBag<INetworkEventSubscriber>();

        /// <summary>
        /// Anzahl von Zuhörern, die bei neuen Ereignissen benachrichtigt werden
        /// </summary>
        public int SubscriberCount => Subscribers.Count;



        /// <summary>
        /// Registriert ein neues Ereignis beim Aggregator
        /// </summary>
        /// <param name="e">Das neue Ereignis</param>
        public void Report(NetworkEvent e)
        {
            this.Events.Enqueue(e);
            this._semaphore.Set();
        }

        private ManualResetEvent _semaphore = new ManualResetEvent(false);

        private  void DoEventProcessing()
        {
            while(this.ContinueProcessing)
            {
                this._semaphore.WaitOne();

                while (this.Events.TryDequeue(out NetworkEvent e))
                {
                    foreach (var subscriber in this.Subscribers)
                    {
                        try
                        {
                            subscriber.Notify(e).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogDebug("Fehler beim Event processing: " + ex.Message);
                        }
                    }
                }

                this._semaphore.Reset();
            }
        }
    }


    public abstract class NetworkEvent
    {

        /// <summary>
        /// ZWave Node ID des Gerätes, welches das Ereignis ausgelöst hat
        /// </summary>
        public byte SourceNodeId { get; }

        /// <summary>
        /// Zeitpunkt, zu welchem Das Ereignis ausgelöst wurde
        /// </summary>
        public DateTime CreationDate { get; }

        /// <summary>
        /// Name der veränderten Eigenschaft
        /// </summary>
        public string Property { get; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newVal"></param>
        public NetworkEvent(byte source, [CallerMemberName] string caller = null)
        {
            this.SourceNodeId = source;
            this.CreationDate = DateTime.Now;
            this.Property = caller;
        }
    }

    /// <summary>
    /// Abstrakte Darstellung eines Netzwerkereignisses aus dem ZWave Netzwerk
    /// </summary>
    /// <typeparam name="TValue">Typ des Wertes</typeparam>
    public  class NetworkEvent<TValue> : NetworkEvent
    {

        /// <summary>
        /// Der neue Wert
        /// </summary>
        public TValue NewValue { get; }


        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newVal"></param>
        public NetworkEvent(byte source, TValue newVal, [CallerMemberName] string caller = null) : base(source, caller)
        {
            this.NewValue = newVal;
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn ein HW-Button gedrückt wird
    /// </summary>
    public class ClickedEvent : NetworkEvent
    {
        public byte SceneId { get;  }
        public ClickedEvent(byte source, byte sceneId, [CallerMemberName] string caller = null) : base(source, caller)
        {
            this.SceneId = sceneId;
        }
    }

    /// <summary>
    /// Event der ausgelöst wird, wenn ein Timerereignise eintritt
    /// </summary>
    public class TimerEvent : NetworkEvent
    {
        public TimerEvent(byte source, [CallerMemberName] string caller = null) : base(source, caller)
        {
        }
    }

    public class PersonAvailabilityEvent : NetworkEvent
    {
        public long Id_Person { get; private set; }
        public bool IsAvailable { get; private set; }
        public string PersonName { get; private set; }
        public PersonAvailabilityEvent(long personId, string personName, bool newState) : base(0, "PersonAvailability")
        {
            this.Id_Person = personId;
            this.IsAvailable = newState;
            this.PersonName = personName;
        }
    }
    public class PersonGeoFenceEvent : NetworkEvent
    {
        public long Id_Person { get; private set; }
        public IGeoFence CurrentGeoFence { get; private set; }
        public IGeoFence PreviousGeoFence { get; private set; }
        public string PersonName { get; private set; }
        public PersonGeoFenceEvent(long personId, string personName, IGeoFence newState, IGeoFence previousGeoFence) : base(0, "PersonGeoFence")
        {
            this.Id_Person = personId;
            this.CurrentGeoFence = newState;
            this.PersonName = personName;
            this.PreviousGeoFence = previousGeoFence;
        }
    }

    public class ImpftermineChangedEvent : NetworkEvent
    {
        public bool IsTerminAvailable { get; private set; }
        public ImpftermineChangedEvent(bool newState) : base(0, "ImpftermineChanged")
        {
            this.IsTerminAvailable = newState;
        }
    }


    public class AirQualityChangedEvent : NetworkEvent
    {
        public AirQualityChangedEvent(byte source ,
            AirQualitityLevel level, string msg, string roomName, [CallerMemberName] string caller = null) : base(source, caller)
        {
            this.Level = level;
            this.Message = msg;
            this.RoomName = roomName;
        }

        public AirQualitityLevel Level { get;  }
        public string Message { get;  }
        public string RoomName { get; }
    }
}
