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

            try
            {
                string username = configuration["FritzBox:Username"] ?? "";
                string password = configuration["FritzBox:Password"] ?? "";
                string explicitHost = configuration["FritzBox:Host"] ?? "192.168.178.1";

                if (_initialized)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("FritzBox credentials are missing. Host loading may fail.");
                }

                _logger.LogInformation("Initializing HomeNetworkService. Explicit Fritz host: {FritzHost}", explicitHost);

                var devices = await FritzDevice.LocateDevicesAsync();
                _logger.LogInformation("Fritz discovery found {DiscoveredDeviceCount} device(s).", devices.Count);

                foreach (var device in devices)
                {
                    try
                    {
                        device.Credentials = new System.Net.NetworkCredential(username, password);
                        HostsClient client = device.GetServiceClient<HostsClient>();
                        _FritzboxHosts.Add(client);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to initialize discovered Fritz device client.");
                    }
                }

                if (_FritzboxHosts.Count == 0)
                {
                    await TryAddExplicitHostClient(explicitHost, username, password);
                }

                _logger.LogInformation("HomeNetworkService initialized with {HostClientCount} Fritz host client(s).", _FritzboxHosts.Count);

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
            }
            finally
            {
                _initializing = false;
            }
        }

        private async Task TryAddExplicitHostClient(string explicitHost, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(explicitHost))
            {
                _logger.LogWarning("No explicit Fritz host configured and no devices discovered.");
                return;
            }

            var candidateBaseUrls = new[]
            {
                $"https://{explicitHost}:49443",
                $"http://{explicitHost}:49000"
            };

            foreach (var baseUrl in candidateBaseUrls)
            {
                try
                {
                    var client = new HostsClient(baseUrl, 10000, username, password);
                    ushort hostCount = await client.GetHostNumberOfEntriesAsync();
                    _FritzboxHosts.Add(client);
                    _logger.LogInformation("Connected to Fritz host using explicit endpoint {BaseUrl}. Host entries: {HostCount}", baseUrl, hostCount);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed connecting to explicit Fritz endpoint {BaseUrl}", baseUrl);
                }
            }

            _logger.LogError("Could not initialize any Fritz host client. Discovery and explicit endpoint attempts failed.");
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
                _logger.LogDebug("Loaded {DeviceCount} home network host entries.", devices.Count);
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
                ushort numHosts;
                try
                {
                    numHosts = await fritzBox.GetHostNumberOfEntriesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch host count from Fritz host client.");
                    continue;
                }

                for (ushort i = 0; i < numHosts; i++)
                {
                    try
                    {
                        HostEntry entry = await fritzBox.GetGenericHostEntryAsync(i);
                        result.Add(new HomeNetworkHost(entry.HostName, entry.MACAddress, entry.Active, entry.IPAddress?.ToString() ?? ""));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to read Fritz host entry at index {Index}.", i);
                    }
                }
            }
            return result.DistinctBy(item => item.Hostname).ToList();
        }
    }
}

