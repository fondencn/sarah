using Sarah.API.BusinessObjects;

namespace Sarah.LocationServer.Model
{
    public class LocationServiceEntries
    {
        public static LocationServiceEntries Instance { get; } = new LocationServiceEntries();

        private readonly Dictionary<string, Queue<LocationServiceEntry>> _Items = new Dictionary<string, Queue<LocationServiceEntry>>();

        private const int MAX_ITEMS_PER_PERSON = 1000;

        private LocationServiceEntries()
        {

        }

        public void Add(LocationServiceEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (_Items.ContainsKey(entry.PersonName))
            {
                _Items[entry.PersonName].Enqueue(entry);
                if (_Items[entry.PersonName].Count > MAX_ITEMS_PER_PERSON)
                {
                    _Items[entry.PersonName].Dequeue();
                }
            } 
            else
            {
                Queue<LocationServiceEntry> items = new Queue<LocationServiceEntry>();
                items.Enqueue(entry);
                _Items.Add(entry.PersonName, items);
            }
        }

        public LocationServiceEntry[]? this[string personName]
        {
            get
            {
                Queue<LocationServiceEntry>? values;
                if (! _Items.TryGetValue(personName, out values))
                {
                    values = null;
                }
                return values?.ToArray();
            }
        }

        public void Clear()
        {
            this._Items.Clear();
        }
    }
}
