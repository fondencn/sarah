using PS.FritzBox.API;
using PS.FritzBox.API.LANDevice;

namespace Sarah.Persons.WebApi.Services
{
    public class HomeNetworkService
    {
        public HomeNetworkService(ILogger<HomeNetworkService> logger)
        {
            _logger = logger;
        }
        private readonly ILogger<HomeNetworkService> _logger;

        /// <summary>
        ///  Update-Intervall für die Aktualisierung der verbundenen Geräte
        /// </summary>
        private const int UPDATE_WAIT_TIME = 60000;

        private bool _initialized = false;
        private bool _initializing = false;


        private readonly List<HostsClient> _FritzboxHosts = new List<HostsClient>();


        private Task? UpdateTask { get; set; }
        private CancellationTokenSource? UpdateCancellationTokenSource { get; set; }

        private List<HomeNetworkHost>? _knownHosts = null;
        private object _knownHostsLock = new object();
        public IReadOnlyCollection<HomeNetworkHost>? KnownHosts
        {
            get
            {
                IReadOnlyCollection<HomeNetworkHost>? res;
                lock (_knownHostsLock)
                {
                    res = _knownHosts?.AsReadOnly();
                }
                return res;
            }
        }


        public async Task Initialize(IConfiguration configuration)
        {
            if (_initializing)
            {
                return;
            } 
            else 
            {
                _initializing = true;
            }
            
            if (_initialized)
            {
                return;
            }

            var devices = await FritzDevice.LocateDevicesAsync();
            foreach (var device in devices)
            {
                /* Credentials form config */
                string username = configuration["FritzBox:Username"] ?? "";
                string password = configuration["FritzBox:Password"] ?? "";
                device.Credentials = new System.Net.NetworkCredential(username, password);

                //var client = await device.GetServiceClient<WANCommonInterfaceConfigClient>(settings);
                //OnlineMonitorInfo monitor = await client.GetOnlineMonitorAsync(0);


                HostsClient client = device.GetServiceClient<HostsClient>();
                _FritzboxHosts.Add(client);
            }


            CancellationTokenSource cts = new CancellationTokenSource();
            this.UpdateCancellationTokenSource = cts;
            this.UpdateTask = Task.Run(async () =>
            {
                while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
                {
                    await UpdateConnectedHosts();
                    /* Alle 60 Sekunden */
                    await Task.Delay(UPDATE_WAIT_TIME, this.UpdateCancellationTokenSource.Token);
                }
            }, cts.Token);


            _initialized = true;
            _initializing = false;
        }


        /// <summary>
        /// dtor (managed)
        /// </summary>
        ~HomeNetworkService()
        {
            if (this.UpdateTask != null && this.UpdateTask.Status == TaskStatus.Running && this.UpdateCancellationTokenSource != null)
            {
                this.UpdateCancellationTokenSource.Cancel();
            }
        }

        private async Task UpdateConnectedHosts()
        {
            try
            {
                List<HomeNetworkHost> devices = await GetConnectedDevices();
                lock (_knownHostsLock)
                {
                    if (_knownHosts == null)
                    {
                        _knownHosts = devices;
                    }
                    else
                    {
                        foreach (HomeNetworkHost device in devices)
                        {
                            var existing = _knownHosts.FirstOrDefault(item => String.Equals(item.Hostname, device.Hostname, StringComparison.OrdinalIgnoreCase));
                            if (existing == null)
                            {
                                _knownHosts.Add(device);
                            }
                            else
                            {
                                existing.IsConnected = device.IsConnected;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Verbundenen Heimnetzgeräte (LAN/WIFI)");
            }
        }

        private async Task<List<HomeNetworkHost>> GetConnectedDevices()
        {
            List<HomeNetworkHost> result = new List<HomeNetworkHost>();
            List<HostsClient> threadSafeList = [.. _FritzboxHosts];
            foreach (var fritzBox in threadSafeList)
            {
                ushort numHosts = await fritzBox.GetHostNumberOfEntriesAsync();

                for (ushort i = 0; i < numHosts; i++)
                {
                    try
                    {
                        HostEntry entry = await fritzBox.GetGenericHostEntryAsync(i);
                        result.Add(new HomeNetworkHost(entry.HostName, entry.MACAddress, entry.Active, entry.IPAddress?.ToString() ?? ""));
                    }
                    catch 
                    {
                        // in FritzApi 1.2.4 ist hier ein Bug: Der Zugriff bei mehr als 1 Fritzbox für zu Array INdex out of Range
                        //Logger.Instance.LogException(ex);
                    }
                }
            }
            return result.DistinctBy(item => item.Hostname).ToList();
        }
    }
}

