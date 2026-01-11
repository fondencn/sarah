
namespace Sarah.Persons
{
    public class HomeNetworkHost
    {
        private bool _isConnected;

        public string Hostname { get; }

        public string HostnameWithOnlineState => (IsConnected ? "✔" : "") + Hostname;

        public DateTime? LastConnectedStateChanged { get; private set; }

        public string MAC { get; }
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if(_isConnected != value)
                {
                    _isConnected = value;
                    this.LastConnectedStateChanged = DateTime.Now;
                }
            }
        }
        public string IP { get; }

        public HomeNetworkHost(string hostname, string mac, bool isConnected, string ip)
        {
            this.Hostname = hostname;
            this.MAC = mac;
            this.IsConnected = isConnected;
            this.IP = ip;
            this.LastConnectedStateChanged = DateTime.Now;
        }

        public override string ToString()
        {
            return this.HostnameWithOnlineState ?? string.Empty;
        }
    }
}

