using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Logging;
using Sarah.DeviceService.Model;
using Sarah.DeviceService.Model.Extensions;
using Sarah.DeviceService.Model.ParameterProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;
using Sarah.API.Interfaces.Services;
using System.IO;

namespace Sarah.DeviceService
{
    public class DeviceService : IDeviceService
    {
        private ZWaveController Controller { get; set; }
        private NodeCollection Nodes { get; set; }

        public string StatusMessage { get; private set; }

        private string _serialPortName;

        public string SerialPortName => _serialPortName;

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

        public INetworkElement GetNetworkItem(byte nodeID) => this.NetworkElements.FirstOrDefault(item => item.NodeID == nodeID);


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
        /// ctor creates and starts the ZWAve service component
        /// </summary>  
        public DeviceService(INodeFactory nodeFactory)
        {
            this._serialPortName = ReadConfig();
            this.Start(nodeFactory).Wait();
        }

        /// <summary>
        /// dtor
        /// </summary>
        ~DeviceService()
        {
            this.Controller?.Close();
            if (this.UpdateTask != null && this.UpdateTask.Status == TaskStatus.Running)
            {
                this.UpdateCancellationTokenSource.Cancel();
            }
        }


        /// <summary>
        /// Read ZWAve Hardware settings from config file serialport.cfg. 
        /// Defaults to COM7 if no config file is present.
        /// </summary>
        /// <returns>Serial port name to be used by Zwave hardware</returns>
        private static string ReadConfig()
        {
            string portname = null;
            if(File.Exists("serialport.cfg"))
            {
                portname = File.ReadAllText("serialport.cfg");
            }


            string[] availablePorts = System.IO.Ports.SerialPort.GetPortNames();

            if (String.IsNullOrWhiteSpace(portname))
            {
                portname = availablePorts?.FirstOrDefault() ?? "COM7"; //Fallback: Windows Development Default
            }


            Logger.Instance.LogDebug("Available Serial Ports: " + String.Join(", ", availablePorts));
            Logger.Instance.LogDebug("Selected Serial Port:   " + portname);

            return portname;
        }


        /// <summary>
        /// Startet das Netzwerk  auf dem übergebenen Defaultport
        /// </summary>
        /// <returns></returns>
        private async Task Start(INodeFactory nodeFactory)
        {
            ZWaveController controller = null;
            try
            {
                if (_serialPortName == null)
                {
                    throw new InvalidOperationException("Kein SerialPort übergeben - erster Aufruf muss mit COM-Port als Parameter erfolgen");
                }
                Logger.Instance.LogDebug("Starting Controller on serial port " + _serialPortName);
                ISerialPort serialPort;
                try
                {
                    serialPort = SerialPortFactory.Instance.Create(_serialPortName);
                } catch
                {
                    serialPort = null;
                }
                this.StatusMessage = null;

                if (serialPort != null)
                {
                    controller = new ZWaveController(serialPort);

                    // open the controller
                    controller.Open();
                    this.Controller = controller;

                    // Register Logging to text file
                    if (Debugger.IsAttached)
                    {
                        this.Controller.Channel.Log = Console.Out;
                    }
                }

                await UpdateNodeList(nodeFactory);

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
                        Logger.Instance.LogException(ex);
                        this.StatusMessage = "OK - ZWave Network with unkown HomeID (" + ex.Message + " )";
                    }
                }
                else
                {
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
                Logger.Instance.LogDebug(ex.Message);
                Logger.Instance.LogDebug(ex.StackTrace);
                this.StatusMessage = ex.Message + " " + ex.StackTrace;
                if (!Debugger.IsAttached)
                {
                    throw;
                }
            }
        }

        private void Controller_Error(object sender, ZWave.ErrorEventArgs e)
        {
            Logger.Instance.LogError("Controller_Error: " + e.Error?.Message);
        }

        private Task UpdateTask { get; set; }
        private CancellationTokenSource UpdateCancellationTokenSource { get; set; }

        /// <summary>
        /// Aktualisiert die interne Liste der verbundenen ZWave-Knoten
        /// </summary>
        /// <returns>Task</returns>
        private async Task UpdateNodeList(INodeFactory nodeFactory)
        {
            Logger.Instance.LogDebug("Updating NodeList...");
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
                    NetworkElement nodeElement = nodeFactory.CreateByNodeId(n.NodeID);
                    if (nodeElement == null)
                    {
                        Logger.Instance.LogDebug("Adding Zwave Node " + n.NodeID + " as  UNKNOWN ELEMENT (add to NodeFactory now!)...");
                        networkElements.Add(new UnknownElement(n.NodeID));
                    }
                    else
                    {
                        Logger.Instance.LogDebug("Adding Zwave Node " + n.NodeID + " as " + nodeElement.GetType().Name + "...");
                        networkElements.Add(nodeElement);
                    }
                    await nodeElement.InitializeAsync(this);
                }
            }

            /* jetzt die Nicht-ZWave Geräte */
            foreach (byte nodeId in nodeFactory.GetNonZwaveNodeIds().ToList())
            {
                NetworkElement nodeElement = nodeFactory.CreateByNodeId(nodeId);
                if (nodeElement == null)
                {
                    Logger.Instance.LogDebug("Adding non-Zwave Node " + nodeId + " as UNKNOWN Element...");
                    networkElements.Add(new UnknownElement(nodeId));
                }
                else
                {
                    Logger.Instance.LogDebug("Adding non-Zwave Node " + nodeId + " as " + nodeElement.GetType().Name + "...");
                    networkElements.Add(nodeElement);
                    await nodeElement.InitializeAsync(this);
                }
            }

            this.NetworkElements.Clear();
            this.NetworkElements.AddRange(networkElements);

            if(!Debugger.IsAttached && (this.NetworkElements.Count != this.Nodes.Count() + nodeFactory.GetNonZwaveNodeIds().Count()))
            {
                var missing = this.Nodes.Where(item => !this.NetworkElements.Any(item2 => item.NodeID == item2.NodeID)).ToList();
                throw new InvalidOperationException("Fehler beim Initialisieren: es wurden nicht alle Knoten initialisiert (ist=" + this.NetworkElements.Count + ", Soll=" + this.Nodes.Count() + ")");
            }

            Logger.Instance.LogInfo("UpdateNodeList done for " + this.NetworkElements.Count + " nodes.");
        }

        /// <summary>
        /// Ermittelt einen Parameterprovider für den angegebenen Typ.
        /// Gibt NULL zurück, wenn es keinen bekannten Parametersatz für ein Gerät mit diesem Typ gibt
        /// </summary>
        /// <param name="specificType"></param>
        /// <returns></returns>
        public IParameterProvider GetParameterProvider(KnownDeviceTypes specificType)
        {
            IParameterProvider result;
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
            Node n = this.GetNodeInternal(nodeId);
            if (n != null)
            {
                Color c = n.GetCommandClass<Color>();
                System.Drawing.Color cc = ColorConverter.FromHex(color);

                /* via https://aeotec.freshdesk.com/support/solutions/articles/6000202221-led-bulb-6-multi-color-user-guide-
                 * Switch Color SET Command Class.

                    LED Bulb 6 uses SWITCH COLOR Command Class to allow you to change between Warm White, Cold White, or a mixture of RGB colors. Warm White takes the highest priority and will default to this setting on factory reset values.

                    Capability ID
                    Color
                    0
                    Warm White
                    1
                    Cold White
                    2
                    Red
                    3
                    Green
                    4
                    Blue

                    Notes:
                    Warm white takes highest priority over all other colors.
                    In order for Cold White to appear, Warm White must be disabled or set to 0% intensity
                    For RGB color mixes to work, both Cold White and Warm White must be disabled or set to 0% intensity.
                 *
                 */
                ColorComponent warmWhite = new ColorComponent(ColorComponentType.WarmWhite, 0);
                ColorComponent coldWhite = new ColorComponent(ColorComponentType.CoolWhite, 0);
                ColorComponent r = new ColorComponent(ColorComponentType.Red, cc.R);
                ColorComponent g = new ColorComponent(ColorComponentType.Green, cc.G);
                ColorComponent b = new ColorComponent(ColorComponentType.Blue, cc.B);

                await c.Set(new ColorComponent[] { warmWhite, coldWhite, r, g, b });
            }
        }

        public async Task SetLampBrightness(byte nodeId, byte brightness)
        {
            Node n = this.GetNodeInternal(nodeId);
            if (n != null)
            {
                Basic basic = n.GetCommandClass<Basic>();
                await basic.Set(brightness);


                //Color c = n.GetCommandClass<Color>();


                //ColorComponent fade = new ColorComponent(1, 2); // FadeIn FadeOut; 1= DirectColor
                //ColorComponent r = new ColorComponent(2, 255);
                //ColorComponent g = new ColorComponent(3, 10);
                //ColorComponent b = new ColorComponent(4, 10);
                //await c.Set(new ColorComponent[] { fade, r, g, b });
            }
        }


        internal Node GetNodeInternal(byte nodeid)
        {
            Node n = this.Nodes?.FirstOrDefault(item => item.NodeID == nodeid);
            if (n == null)
            {
                Logger.Instance.LogWarning("GetNode(" + nodeid + "): nicht gefunden!");
            }
            return n;
        }

        public async Task<IAssociationGroup[]> GetAssociationGroups(byte nodeID)
        {
            Node n = GetNodeInternal(nodeID);
            if (n == null)
            {
                return null;
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
                    Logger.Instance.LogException(ex);
                    return null;
                }
            }
        }
        public async Task SetAssociationGroup(byte nodeID, byte groupId, byte[] nodeIds)
        {
            Node n = GetNodeInternal(nodeID);
            if (n == null)
            {
                Logger.Instance.LogError("SetAssociationGroup: ZWAVE Node " + nodeID + " nicht gefunden");
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
                    Logger.Instance.LogException(ex);
                }
            }
        }

        public INode GetNode(byte nodeid) => new NodeWrapper(GetNodeInternal(nodeid));

        public IEnumerable<SelfTestResult> RunSelfTest()
        {
            if(String.IsNullOrEmpty(this._serialPortName))
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
                Logger.Instance.LogDebug(el.NodeID + "\t|\t" + String.Join(" | ", neighborIds));
                Logger.Instance.LogDebug("--------------------------------------------------------");
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

    }
}
