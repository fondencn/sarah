using Sarah.API.BusinessObjects;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.API.Interfaces;
using Sarah.DeviceService.Model;
using Sarah.DeviceService.Model.Extensions;
using Sarah.DeviceService.Model.ParameterProviders;
using System.Diagnostics;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;
using Sarah.API.Interfaces.Services;
using Sarah.DeviceService.WebApi.Extensions;
using Microsoft.Extensions.Hosting;
using Sarah.DeviceService.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Sarah.DeviceService
{
    public class DeviceService : BackgroundService, IDeviceService
    {
        private readonly IConfiguration _configuration;
        private readonly INodeFactory _nodeFactory;
        private readonly ILogger<DeviceService> _logger;
        private readonly NetworkElementPublisher _publisher;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Task? UpdateTask { get; set; }
        private CancellationTokenSource? UpdateCancellationTokenSource { get; set; }


        /// <summary>
        /// ctor creates and starts the ZWAve service component
        /// </summary>  
        public DeviceService(INodeFactory nodeFactory, IConfiguration config, NetworkElementPublisher publisher, ILogger<DeviceService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            this._configuration = config;
            this._nodeFactory = nodeFactory;
            this._publisher = publisher;
            this._logger = logger;
            this._serviceScopeFactory = serviceScopeFactory;
        }


        private ZWaveController? Controller { get; set; }
        private NodeCollection? Nodes { get; set; }

        public string StatusMessage { get; private set; } = "";


        public string SerialPortName => _configuration["ZWave:SerialPortName"] ?? throw new InvalidOperationException("ZWave:SerialPortName configuration is missing");

        private List<NetworkElement> NetworkElements { get; } = new List<NetworkElement>();

        public IEnumerable<IDoorSensor> DoorSensors => NetworkElements.OfType<DoorSensor>();
        public IEnumerable<ISmokeSensor> SmokeSensors => NetworkElements.OfType<SmokeSensor>();
        public IEnumerable<ILamp> Lamps => NetworkElements.OfType<ILamp>();
        public IEnumerable<IThermoElement> Heatings => NetworkElements.OfType<ThermoElement>();
        public IEnumerable<IControllerElement> Controllers => NetworkElements.OfType<ControllerElement>();
        public IEnumerable<IWallController> WallControllers => NetworkElements.OfType<WallController>();
        public IEnumerable<IWallPlug> WallPlugs => NetworkElements.OfType<WallPlug>();
        public IEnumerable<IMultiSensor> Sensors => NetworkElements.OfType<MultiSensor>();
        public IEnumerable<IBatterySensor> BatterySensors => NetworkElements.OfType<IBatterySensor>();
        public IEnumerable<ITemperatureSensor> TemperatureSensors => NetworkElements.OfType<ITemperatureSensor>();
        public IEnumerable<IUnknownElement> UnknownElements => NetworkElements.OfType<UnknownElement>();
        public IEnumerable<IDefectElement> DefectElements => NetworkElements.OfType<DefectElement>();
        public IEnumerable<IGPSTracker> GPSTrackers => NetworkElements.OfType<IGPSTracker>();
        public IEnumerable<NetworkElement> Elements => NetworkElements.AsReadOnly();

        public INetworkElement? GetNetworkItem(byte nodeID) => this.NetworkElements?.FirstOrDefault(item => item.NodeID == nodeID);

        /// <summary>
        /// Map für bestimmte Parameterprovider
        /// </summary>
        private Dictionary<KnownDeviceTypes, IParameterProvider> _KnownParameterProviders { get; } = new Dictionary<KnownDeviceTypes, IParameterProvider>
        {
            {KnownDeviceTypes.FibaroMotionSensor, new FibaroMotionSensorParameterProvider() }, //MotionSensor: Das Auge
            {KnownDeviceTypes.AeotecZStick, new AeotecZstickParameterProvider() }, //USB Controller Stick
            {KnownDeviceTypes.FibaroHeatController, new FibaroHeatControllerParameterProvider() }, // Heizung
            {KnownDeviceTypes.AeotecLedBulb, new AeotecLedBulbParameterProvider() }, // Lampe
            {KnownDeviceTypes.FibaroRGBWController2, new FibaroRGBWController2ParameterProvider() }, // LED Strip Controller
            {KnownDeviceTypes.AeotecThermostat, new AeotecThermostatParameterProvider() }, // Heizkörperthermostat von Aeotec
            {KnownDeviceTypes.AeotecSmartSwitch7, new AeotecSmartSwitch7ParameterProvider() }, // Heizkörperthermostat von Aeotec
            {KnownDeviceTypes.FibaroDoorWindowSensor2, new FibaroDoorWindowSensor2ParameterProvider() }, // Türsensor2 von Fibaro
            {KnownDeviceTypes.EutronicAirQualitySensor, new EutronicAirQualitySensorParameterProvider() }, // Luftsensor von Eutronic
            {KnownDeviceTypes.FibaroWalliSwitch, new FibaroWalliParameterProvider() }, // Luftsensor von Eutronic
            {KnownDeviceTypes.PoppWallController, new PoppWallControllerParameterProvider() }, // 4-fach Lichtschalter von Popp
            {KnownDeviceTypes.FibaroSmokeSensor, new FibaroSmokeParameterProvider() }, // Rauchmelder von Fibaro
            {KnownDeviceTypes.FibaroKeyFob, new FibaroKeyFobParameterProvider() } // Fernbedienung von Fibaro
        };

    
        /// <summary>
        /// dtor
        /// </summary>
        ~DeviceService()
        {
            this.Controller?.Close();
            if (this.UpdateTask != null && UpdateCancellationTokenSource != null && this.UpdateTask.Status == TaskStatus.Running)
            {
                this.UpdateCancellationTokenSource.Cancel();
            }
        }




        /// <summary>
        /// Startet das Netzwerk  auf dem übergebenen Defaultport
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            try
            {
                if (SerialPortName == null)
                {
                    throw new InvalidOperationException("Missing configuration for ZWave serial port");
                }
                _logger.LogInformation("Starting Controller on serial port " + SerialPortName);
                ISerialPort? serialPort;
                try
                {
                    serialPort = SerialPortFactory.Instance.Create(SerialPortName);
                    ZWaveController controller = new ZWaveController(serialPort);

                    // open the controller
                    controller.Open();
                    this.Controller = controller;
                } 
                catch(Exception ex)
                {
                    serialPort = null;
                    this.StatusMessage = ex.Message;
                    _logger.LogError("Error opening serial port at " + SerialPortName + ": " + ex.Message);
                    _logger.LogError(ex, "An error occurred");
                }


                await UpdateNodeList();

                /////////////////// DISPOSE PROBLEM: NODES SIND XFACH DA!!!!!!!!!!!!!!!!!!!!!!
                //CancellationTokenSource cts = new CancellationTokenSource();
                //this.UpdateCancellationTokenSource = cts;
                //this.UpdateTask = Task.Run(async () =>
                //{
                //    while (!this.UpdateCancellationTokenSource.Token.IsCancellationRequested)
                //    {
                //        await UpdateNodeList();
                //        /* Alle 60 Minuten */
                //        await Task.Delay(60 * 60 * 1000);
                //    }
                //}, cts.Token);

                if (this.Controller != null)
                {
                    try
                    {
                        this.Controller.Error += Controller_Error;
                        uint homeid = await this.Controller.GetHomeID();
                        string controllerversion = await this.Controller.GetVersion();
                        this.StatusMessage = "OK - ZWave Network with HomeID " + (homeid) + ", ControllerVersion=" + (controllerversion);
                    } catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred");
                        this.StatusMessage = "OK - ZWave Network with unkown HomeID (" + ex.Message + " )";
                    }
                }
                else
                {
                    _logger.LogWarning("No ZWave Controller available, device service started without ZWave functionality");
                    this.StatusMessage = "WARN - Kein ZWave Controller vorhanden";
                }
            }
            catch (Exception ex)
            {
                if (this.Controller != null)
                {
                    this.Controller.Close();
                    this.Controller = null;
                }
                this.Nodes = null;
                _logger.LogError("Device service startup failed {Exception}", ex.Message);
                this.StatusMessage = ex.Message;

                throw; // No catch here, let the exception bubble up - this will cause the service to stop and can be used by hosting environment to trigger a restart (e.g., via Kubernetes liveness probe)
            } 
        }

        private void Controller_Error(object? sender, ZWave.ErrorEventArgs e)
        {
            _logger.LogError("Controller_Error: " + e.Error?.Message);
        }

        /// <summary>
        /// Aktualisiert die interne Liste der verbundenen ZWave-Knoten
        /// </summary>
        /// <returns>Task</returns>
        private async Task UpdateNodeList()
        {
            _logger.LogInformation("Updating NodeList...");
            this.StatusMessage = "Verbundene Geräte werden Initialisiert...";
            // get the included nodes
            if (this.Controller != null)
            {
                this.Nodes = await this.Controller.GetNodes();
            }

            List<NetworkElement> networkElements = new List<NetworkElement>();

            /* ZWAve Geräte laden, sofern ein Controller vorhanden ist */
            if (this.Nodes != null)
            {
                foreach (Node n in this.Nodes)
                {
                    NetworkElement? nodeElement = _nodeFactory.CreateByNodeId(n.NodeID);
                    if (nodeElement == null)
                    {
                        _logger.LogInformation("Adding Zwave Node " + n.NodeID + " as  UNKNOWN ELEMENT (add to NodeFactory now!)...");
                        networkElements.Add(new UnknownElement(n.NodeID, _publisher, null));
                    }
                    else
                    {
                        _logger.LogInformation("Adding Zwave Node " + n.NodeID + " as " + nodeElement.GetType().Name + "...");
                        networkElements.Add(nodeElement);
                        await nodeElement.InitializeAsync(this, _configuration);
                    }
                }
            }

            /* jetzt die Nicht-ZWave Geräte */
            foreach (byte nodeId in _nodeFactory.GetNonZwaveNodeIds().ToList())
            {
                NetworkElement? nodeElement = _nodeFactory.CreateByNodeId(nodeId);
                if (nodeElement == null)
                {
                    _logger.LogInformation("Adding non-Zwave Node " + nodeId + " as UNKNOWN Element...");
                    networkElements.Add(new UnknownElement(nodeId, _publisher, null));
                }
                else
                {
                    _logger.LogInformation("Adding non-Zwave Node " + nodeId + " as " + nodeElement.GetType().Name + "...");
                    networkElements.Add(nodeElement);
                    await nodeElement.InitializeAsync(this, _configuration);
                }
            }

            this.NetworkElements.Clear();
            this.NetworkElements.AddRange(networkElements);

            if(this.NetworkElements.Count != (this.Nodes?.Count() ?? 0) + _nodeFactory.GetNonZwaveNodeIds().Count())
            {
                var missing = this.Nodes!.Where(item => !this.NetworkElements.Any(item2 => item.NodeID == item2.NodeID)).ToList();
                _logger.LogError("Fehler beim Initialisieren: es wurden nicht alle Knoten initialisiert! Fehlende Knoten: {MissingNodes}",
                    String.Join(",", missing.Select(item => item.NodeID)));
                // eigentlich schlecht, kann aber passieren wenn gerade kein ZWave Controller am Gerät steckt. Dann nur Non-ZWave Geräte nutzen...
            }

            _logger.LogInformation("UpdateNodeList done for " + this.NetworkElements.Count + " nodes.");
        }

        /// <summary>
        /// Ermittelt einen Parameterprovider für den angegebenen Typ.
        /// Gibt NULL zurück, wenn es keinen bekannten Parametersatz für ein Gerät mit diesem Typ gibt
        /// </summary>
        /// <param name="specificType"></param>
        /// <returns></returns>
        public IParameterProvider? GetParameterProvider(KnownDeviceTypes specificType)
        {
            IParameterProvider? result;
            if(!_KnownParameterProviders.TryGetValue(specificType, out result))
            {
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Lampenfarbe (RGB) setzen
        /// </summary>
        /// <param name="nodeId">Zwave node id</param>
        /// <param name="color">Farbe als Hex-String</param>
        /// <returns></returns>
        public async Task SetLampColor(byte nodeId, string color)
        {
            var lamp = this.Lamps.FirstOrDefault(item => item.NodeID == nodeId);
            if(lamp != null)
            {
                await lamp.SetColor(color);
            }
        }

        public async Task SetLampBrightness(long deviceId, byte brightness)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
            if (device == null)
            {
                _logger.LogWarning("SetLampBrightness: no device found for deviceId {DeviceId}", deviceId);
                return;
            }

            var lamp = this.Lamps.FirstOrDefault(item => item.NodeID == device.NodeID);
            if(lamp != null)
            {
                await lamp.SetBrightness(brightness);
            }
        }


        internal Node? GetNodeInternal(byte nodeid)
        {
            Node? n = this.Nodes?.FirstOrDefault(item => item.NodeID == nodeid);
            if (n == null)
            {
                _logger.LogWarning("GetNode(" + nodeid + "): nicht gefunden!");
            }
            return n;
        }

        public async Task<IAssociationGroup[]> GetAssociationGroups(byte nodeID)
        {
            Node? n = GetNodeInternal(nodeID);
            if (n == null)
            {
                return Array.Empty<IAssociationGroup>();
            } 
            else
            {
                try
                {
                    List<IAssociationGroup> groups = new List<IAssociationGroup>();
                    /* Read Group values */
                    Association ass = n.GetCommandClass<Association>();
                    var groupsReport = await ass.GetGroups();
                    for (byte i = 0; i < groupsReport.GroupsSupported; i++)
                    {
                        AssociationReport assForGroup = await ass.Get(i);
                        AssociationGroup foundGroup = new AssociationGroup()
                        {
                            GroupID = assForGroup.GroupID,
                            Nodes = assForGroup.Nodes,
                            MaxNodesSupported = assForGroup.MaxNodesSupported
                        };
                        groups.Add(foundGroup);
                    }

                    return groups.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred");
                    return Array.Empty<IAssociationGroup>();
                }
            }
        }
        public async Task SetAssociationGroup(byte nodeID, byte groupId, byte[] nodeIds)
        {
            Node? n = GetNodeInternal(nodeID);
            if (n == null)
            {
                _logger.LogError("SetAssociationGroup: ZWAVE Node " + nodeID + " nicht gefunden");
            }
            else
            {
                try
                {
                    List<IAssociationGroup> groups = new List<IAssociationGroup>();
                    /* Read Group values */
                    Association ass = n.GetCommandClass<Association>();
                    AssociationReport assForGroup = await ass.Get(groupId);
                    foreach(byte existing in assForGroup.Nodes)
                    {
                        /* Alle existierenden Entfernen, die in der neuen Liste nicht mehr enthalten sind */
                        if(!nodeIds.Any(item => item == existing))
                        {
                            await ass.Remove(groupId, existing);
                        }
                    }
                    foreach (byte node in nodeIds)
                    {
                        /* Alle hinzufügen, die in der neuen Liste drin sind aber bisher nicht zugewiesen sind */
                        if (!assForGroup.Nodes.Any(item => item == node))
                        {
                            await ass.Add(groupId, node);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred");
                }
            }
        }

        public INode? GetNode(byte nodeid) => new NodeWrapper(GetNodeInternal(nodeid));

        public Task<TrackerDto?> GetGpsTrackerByNodeId(byte nodeId)
        {
            var tracker = this.GPSTrackers.FirstOrDefault(t => t.NodeID == nodeId);
            if (tracker == null)
            {
                return Task.FromResult<TrackerDto?>(null);
            }

            var dto = new TrackerDto
            {
                Id = tracker.NodeID,
                Name = tracker.ClassDescription, 
                Position = tracker.Position == null ? null : new PositionDto()
                {
                    Longitude = tracker.Position.Longtitude.Value, 
                    Latitude = tracker.Position.Latitude.Value,
                    IsValid = tracker.Position.IsValid
                }, 
                BatteryLevel = tracker.Battery?.Value  
            };

            return Task.FromResult<TrackerDto?>(dto);
        }

        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if(String.IsNullOrEmpty(this.SerialPortName))
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Keine serielle Schnittstelle für den ZWave Controller definiert oder ZWave Anbindung nicht gestartet.");
            }
            if (this.Controller == null)
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Kein Controller definiert");
            }
            if (this.UnknownElements.Any())
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Unbekannte ZWave Geräte im Netzwerk entdeckt: " + String.Join(",",this.UnknownElements.Select(item => item.NodeID)));
            }
            if (this.Nodes?.Count() < 24)
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Es wurden nicht alle Zwave Geräte gefunden (weniger als 24)");
            }

            if (!(this.GetNetworkItem(25) is WallPlug))
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Gerät 25 ist keine Steckdose, sollte aber eine sein");
            }
            if (!(this.GetNetworkItem(27) is WallPlug))
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Gerät 27 ist keine Steckdose, sollte aber eine sein");
            }
            if (!(this.GetNetworkItem(14) is Lamp))
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Gerät 14 ist keine Lampe, sollte aber eine sein");
            }

            if (!(this.Controllers.Any()))
            {
                yield return new SelfTestResult(true, "ZWave Controller", "Kein ZWave Controller im Netzwerk bekannt");
            }
            /* Für alle Geräte alle Nachbarn holen und schauen, ob eine transitive Verbindung da ist */
            Dictionary<byte, byte[]> adjacentNodesMatrix = new Dictionary<byte, byte[]>();
            List<byte> allNodeIds = this.Elements
                .Where(item => !(item is DefectElement))
                .Select(item => item.NodeID)
                .Distinct()
                .ToList();
            foreach(NetworkElement el in this.Elements.Where(item => !(item is DefectElement)))
            {
                byte[] neighborIds = el.GetNeighbors(this).Result;
                adjacentNodesMatrix.Add(el.NodeID, neighborIds);
                _logger.LogDebug(el.NodeID + "\t|\t" + String.Join(" | ", neighborIds));
                _logger.LogDebug("--------------------------------------------------------");
            }

            byte controllerId = this.Controllers.First().NodeID;
            allNodeIds.Remove(controllerId);
            RemoveRecursive(controllerId, adjacentNodesMatrix, allNodeIds);


            foreach  (byte übrig in allNodeIds.ToArray())
            {
                /* nachschauen, ob deren Neighbors (mindestens einer) nicht mehr in der Listen ist
                 * -> Sensor, hat wenigstens eine Rückwärtsverbindung zum Netzerk
                 */
                var nachbarnDesÜbrigen = adjacentNodesMatrix[übrig];
                if(nachbarnDesÜbrigen.Any(item => !allNodeIds.Contains(item)) )
                {
                    allNodeIds.Remove(übrig);
                }
            }
            if(allNodeIds.Any())
            {
                yield return new SelfTestResult(true, "Zwave Netzwerk", "Die folgenden Geräte sind nicht direkt oder indirekt mit dem Netzwerk verbunden: " + String.Join(",", allNodeIds));
            }
        }

        private void RemoveRecursive(byte node, Dictionary<byte, byte[]> adjacentNodesMatrix, List<byte> allNodeIds)
        {
            foreach (byte directlyConnectednode in adjacentNodesMatrix[node])
            {
                if (allNodeIds.Contains(directlyConnectednode))
                {
                    allNodeIds.Remove(directlyConnectednode);
                    RemoveRecursive(directlyConnectednode, adjacentNodesMatrix, allNodeIds);
                }
            }
        }

        /// <summary>
        /// BackgroundService implementation - starts the DeviceService automatically
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DeviceService background service is starting...");
            
            try
            {
                await Start();
                _logger.LogInformation("DeviceService background service started successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting DeviceService background service.");
            }

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }


        // High-level API methods - not implemented in device service (these are for HTTP clients)
        public Task ToggleLampByRoom(string roomName, string lampName)
        {
            _logger.LogWarning("ToggleLampByRoom called on device service - not implemented");
            return Task.CompletedTask;
        }

        public Task SetLampByRoom(string roomName, string lampName, bool on)
        {
            _logger.LogWarning("SetLampByRoom called on device service - not implemented");
            return Task.CompletedTask;
        }

        public Task SetTemperatureByRoom(string roomName, float temperature)
        {
            _logger.LogWarning("SetTemperatureByRoom called on device service - not implemented");
            return Task.CompletedTask;
        }

        public Task<Sarah.API.BusinessObjects.SpeakerRequests.GetOpenDoorsResponse> GetOpenDoors()
        {
            _logger.LogWarning("GetOpenDoors called on device service - not implemented");
            return Task.FromResult(new Sarah.API.BusinessObjects.SpeakerRequests.GetOpenDoorsResponse());
        }

        public Task<Sarah.API.BusinessObjects.SpeakerRequests.GetDeseaseInfoResponse> GetDeseaseInfo()
        {
            _logger.LogWarning("GetDeseaseInfo called on device service - not implemented");
            return Task.FromResult(new Sarah.API.BusinessObjects.SpeakerRequests.GetDeseaseInfoResponse());
        }

        public Task SetAlarmSchedule(string text, DateTime alarmTime, string speakerHostname)
        {
            _logger.LogWarning("SetAlarmSchedule called on device service - not implemented");
            return Task.CompletedTask;
        }

        public Task<Sarah.API.BusinessObjects.SpeakerRequests.GetAlarmSchedulesResponse> GetAlarmSchedules()
        {
            _logger.LogWarning("GetAlarmSchedules called on device service - not implemented");
            return Task.FromResult(new Sarah.API.BusinessObjects.SpeakerRequests.GetAlarmSchedulesResponse());
        }

        public Task<Sarah.API.BusinessObjects.SpeakerRequests.GetPersonLocationResponse> GetPersonLocation(string personName)
        {
            _logger.LogWarning("GetPersonLocation called on device service - not implemented");
            return Task.FromResult(new Sarah.API.BusinessObjects.SpeakerRequests.GetPersonLocationResponse());
        }

        public Task ActivateScene(string sceneName)
        {
            _logger.LogWarning("ActivateScene called on device service - not implemented");
            return Task.CompletedTask;
        }

        public Task DeactivateScene(string sceneName)
        {
            _logger.LogWarning("DeactivateScene called on device service - not implemented");
            return Task.CompletedTask;
        }

    }
}
