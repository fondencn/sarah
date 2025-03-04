using System;
using System.Collections.Generic;
using Sarah.API.Interfaces;
using Sarah.API.BusinessObjects;
using Sarah.DeviceService.Model;

namespace Sarah.NodeFactory
{
    /// <summary>
    /// Factory Klasse für alle bekannten Netzwerkknoten
    /// </summary>
    public class NodeFactory : INodeFactory
    {
        private readonly IEventProcessingService _events;

        public Nodefactory(IEventProcessingService events)
        {
            this._events = events;
        }

        private readonly Dictionary<byte, Type> _knownNodes = new Dictionary<byte, Type>()
        {
            {1, typeof(ControllerElement)} ,
            {2, typeof(DoorSensor)} ,
            //{3, typeof(DefectElement)} ,/* Kaputter eintrag für Fibaro Heat Controller in Chris Netzwerk */
            //{4, typeof(UnknownElement)} ,
            //{5, typeof(DefectElement)} , /* Kaputter eintrag für Fibaro Heat Controller in Chris Netzwerk */
            {6, typeof(UnknownElement)} ,
            {7, typeof(UnknownElement)} ,
            {8, typeof(UnknownElement)} ,
            {9, typeof(UnknownElement)} ,
            {10, typeof(ZWaveWallPlug)} ,
            {11, typeof(DefectElement)}, // Popp Schalter, Defekter WallController
            {12, typeof(WallController)},  //RedButton
            {13, typeof(MultiSensor)} ,
            {14, typeof(Lamp)} ,
            {15, typeof(ThermoElement)} ,
            {16, typeof(DefectElement)} , //Heizung Wohnzimmer hat seine NodeId vergessen -> neu 40
            {17, typeof(DefectElement)} , //Heizung Schlafzimmer hat seine NodeId vergessen -> neu 39
            {18, typeof(DefectElement)} , //Heizung Lukas hat seine NodeId vergessen -> neu 37
            {19, typeof(ThermoElement)} ,
            {20, typeof(ZWaveWallPlug)} ,
            {21, typeof(Lamp)} ,
            {22, typeof(DoorSensor)} ,
            {23, typeof(DoorSensor)} ,
            {24, typeof(Lamp)} ,
            {25, typeof(ZWaveWallPlug)} ,
            //{26, typeof(DefectElement)} ,/* Kaputter Fibaro WallPlug RMA an Reichelt 02.10.2020  */
            {27, typeof(ZWaveWallPlug)} ,
            {28, typeof(ZWaveWallPlug)} ,
            {29, typeof(DefectElement)} ,  //Fenster Schlafzimmer hat seine NodeId vergessen -> neu 44
            {30, typeof(Lamp)} ,
            {31, typeof(MultiSensor)},
            {32, typeof(WallController)}, // Fibaro Walli Wandschalter
            {33, typeof(DoorSensor)} ,
            {34, typeof(DoorSensor)} ,
            {35, typeof(DoorSensor)} ,
            {36, typeof(DefectElement)} , //Smoke Sensor Resetted
            {37, typeof(ThermoElement)}  , // Heizung Lukas neu
            {38, typeof(SmokeSensor)} ,
            {39, typeof(DefectElement)} , //Heizung Schlafzimmer hat seine NodeId vergessen --> neu 42
            {40, typeof(DefectElement)} , //Heizung Wohnzimmer hat seine NodeId vergessen --> neu 45
            {41, typeof(WallController)}  , // Fibaro Keyfob
            {42, typeof(ThermoElement)}  , // Heizung Schlafzimmer neu 11/2023
            {43, typeof(DoorSensor)}  ,   // Fenster Schlafzimmer neu 01/2024
            {44, typeof(DoorSensor)}  ,   // Fenster Schlafzimmer hat seine NodeId vergessen 29 --> neu 44
            {45, typeof(ThermoElement)}  , // Heizung Wohnzimmer neu
            
            {245, typeof(LoraWanGpsTracker)}, // Seeed T1000 B SenseCap GPS Tracker 003
            {246, typeof(LoraWanGpsTracker)}, // Seeed T1000 B SenseCap GPS Tracker 002
            {247, typeof(LoraWanGpsTracker)}, // Seeed T1000 B SenseCap GPS Tracker 001
            {248, typeof(ShellyWifiLamp)}, // 
            //{249, typeof(LoraWanGpsTracker)}, // Draghino LGT-92 Lorawan GPS Tracker --> verloren
            //{250, typeof(WifiWallPlug)}, // CF 07.01.22 Gerät defekt:  Delock 11826 Wifi Steckdose (ohne Power Report, kann nur an/aus)
            {251, typeof(WifiWallPlug)}, // Delock 11827 Wifi Steckdose (inkl. Power Report)
            {252, typeof(WifiWallPlug)} // Delock 11827 Wifi Steckdose (inkl. Power Report)
        };

        /// <summary>
        /// Nicht-Zwave Geräte, die beim initialisieren nicht über den Controller abgefragt werden können
        /// </summary>
        /// <returns></returns>
        public IEnumerable<byte> GetNonZwaveNodeIds()
        {
            yield return 245;
            yield return 246;
            yield return 247;
            yield return 248;
            yield return 249;
            //yield return 250;// CF 07.01.22 Gerät defekt
            yield return 251;
            yield return 252;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///// ACHTUNG, HARD VERDRAHTETE NODE IDs, DA SICH DER TYP NICHT ERRATEN LÄSST!!!!!!!!!!!!!!!!!!   /////
        ///// TUT NUR IN CHRISTIANS NETZWERK!!!                                                           /////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Mapping von Node-ID zu Sensortyp.
        /// Default: RGBWW
        /// </summary>
        private static Dictionary<byte, LampColorModes> _ColorModeMappings = new Dictionary<byte, LampColorModes>()
        {
            {3, LampColorModes.RGBWW }, //verstecke rgbww glühbirne
            {14,LampColorModes.RGBW }, //LED Kurz Fibaro RGB 2
            {21,LampColorModes.Mono }, //nachtlampe
            {24,LampColorModes.RGBWW }, //Lampe esszimmer
            {30,LampColorModes.RGBW }, //LED Lang Fibaro RGB 2
            {248,LampColorModes.RGBW }, //Shelly Light Bulb
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///// ACHTUNG, HARD VERDRAHTETE HOSTNAMES                                                         /////
        ///// TUT NUR IN CHRISTIANS NETZWERK!!!                                                           /////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Mapping von Node-ID zu Wifi-Hostname
        /// </summary>
        private static Dictionary<byte, string> _WifiDeviceNames = new Dictionary<byte, string>()
        {
            {248, "shellycolorbulb-E8DB84D4E4C2" }, //Wifi Glühbirne
            //{250, "delock-7859" }, // CF 07.01.22 Gerät defekt//Delock 11826 Wifi Steckdose (ohne Power Report, kann nur an/aus)
            {251, "delock-1504"},  // Delock 11827 Wifi Steckdose (inkl. Power Report)
            {252, "delock-0869"}   // Delock 11827 Wifi Steckdose (inkl. Power Report)
        };
        /// <summary>
        /// Mapping von Node-ID zu The Things Network End Device Names
        /// </summary>
        private static Dictionary<byte, string> _TtnDeviceNames = new Dictionary<byte, string>()
        {
            {245, "tracker-t1000-b-003" },
            {246, "tracker-t1000-b-002" },
            {247, "tracker-t1000-b-001" },
            {249, "eui-a840419c1182b044" },
        };

        public NetworkElement? CreateByNodeId(byte nodeId)
        {
            NetworkElement? el;
            if(this._knownNodes.ContainsKey(nodeId))
            {
                Type nodeType = this._knownNodes[nodeId];
                if(nodeType == typeof(Lamp) )
                {
                    /* Ctor Ausnahme: Lampen brauchen zusätzlich noch die Art der Lampe */
                    LampColorModes lampMode = LampColorModes.RGBWW;
                    if(_ColorModeMappings.ContainsKey(nodeId))
                    {
                        lampMode = _ColorModeMappings[nodeId];
                    }
                    el = (NetworkElement?)Activator.CreateInstance(nodeType, nodeId, lampMode, this._events);
                }
                else if (nodeType == typeof(WifiWallPlug))
                {
                    if (_WifiDeviceNames.ContainsKey(nodeId))
                    {
                        /* Ctor Ausnahme: Wifi-Steckdosen brauchen zusätzlich noch einen Hostname, über welchen Sie über das WLAN erreichbar sind */
                        string hostname = _WifiDeviceNames[nodeId];
                        el = (NetworkElement?)Activator.CreateInstance(nodeType, nodeId, hostname, this._events);
                    }
                    else
                    {
                        /* Steckdosen Wifi ohne Hostname tun nicht */
                        el = null;
                    }
                }
                else if (nodeType == typeof(ShellyWifiLamp))
                {
                    if (_WifiDeviceNames.ContainsKey(nodeId))
                    {
                        /* Ctor Ausnahme: Wifi-lAMEPN brauchen zusätzlich noch einen Hostname, über welchen Sie über das WLAN erreichbar sind */
                        string hostname = _WifiDeviceNames[nodeId];
                        el = (NetworkElement?)Activator.CreateInstance(nodeType, nodeId, hostname, this._events);
                    }
                    else
                    {
                        /* STECKDOSEN Wifi ohne Hostname tun nicht */
                        el = null;
                    }
                }
                else if (nodeType == typeof(LoraWanGpsTracker))
                {
                    if (_TtnDeviceNames.ContainsKey(nodeId))
                    {
                        /* Ctor Ausnahme: TTN Device ID mit übergeben */
                        string deviceName = _TtnDeviceNames[nodeId];
                        el = (NetworkElement?)Activator.CreateInstance(nodeType, nodeId, deviceName, this._events);
                    }
                    else
                    {
                        /* TTN Devices ohne ID tun nicht */
                        el = null;
                    }
                }
                else
                {
                    el = (NetworkElement?)Activator.CreateInstance(nodeType, nodeId, this._events);
                }
            }
            else
            {
                el = null;
            }


            return el;



            //try
            //{
            //if(n.NodeID == 5)
            //{
            //    /* Kaputter eintrag für Fibaro Heat Controller in Chris Netzwerk */
            //    DefectElement unknown = new DefectElement(n.NodeID, "Defekter Fibaro Heat Controller");
            //    networkElements.Add(unknown);
            //    continue;
            //}
            //if (n.NodeID == 3)
            //{
            //    /* Kaputter eintrag für Fibaro Heat Controller in Chris Netzwerk */
            //    DefectElement unknown = new DefectElement(n.NodeID, "Versteckte RGB Glühbirne");
            //    networkElements.Add(unknown);
            //    continue;
            //}
            //if (n.NodeID == 26)
            //{
            //    /* Kaputter Fibaro WallPlug RMA an Reichelt 02.10.2020  */
            //    DefectElement unknown = new DefectElement(n.NodeID, "Defekter Fibaro WallPlug");
            //    networkElements.Add(unknown);
            //    continue;
            //}
            //if (n.NodeID == 25 || n.NodeID == 27 || n.NodeID == 28)
            //{
            //    Logger.Instance.LogDebug($"Initializing Node {n.NodeID} as Fibaro Wall Plug (Hard-Coded) ...");
            //    /* Cast Error in ZWave für Fibaro Wall Plugs */
            //    WallPlug c = new WallPlug(n.NodeID);
            //    await c.InitializeAsync();
            //    networkElements.Add(c);
            //    continue;
            //}
            ////if (await n.IsNodeFailed())
            ////{
            ////    Logger.Instance.LogDebug("Skipping node " + n.NodeID + " because it is marked as failed...");
            ////    continue;
            ////}

            /////* Mesh neu berechnen */
            ////await n.RequestNeighborUpdate();

            ///* Jetzt die eigenen Klassen pro aktivem Node zusammensetzen */
            //var proto = await n.GetProtocolInfo();


            //Logger.Instance.LogDebug($"Initializing Node {n.NodeID} as {proto.GenericType} ...");
            //if (proto.GenericType == GenericType.SwitchMultiLevel)
            //{
            //    AbstractLamp lamp = await AbstractLamp.Create(n);
            //    await lamp.InitializeAsync();
            //    networkElements.Add(lamp);
            //}
            //else if (proto.GenericType == GenericType.SensorNotification)
            //{
            //    AbstractSensor sensor = await AbstractSensor.Create(n);
            //    if (sensor != null)
            //    {
            //        await sensor.InitializeAsync();
            //        networkElements.Add(sensor);
            //    }
            //    else
            //    {
            //        UnknownElement unknown = new UnknownElement(n.NodeID, "Unknown AbstractSensor");
            //        networkElements.Add(unknown);
            //    }
            //}
            //else if (proto.GenericType == GenericType.StaticController)
            //{
            //    ControllerElement c = new ControllerElement(n.NodeID);
            //    networkElements.Add(c);
            //}
            //else if (proto.GenericType == GenericType.Thermostat)
            //{
            //    ThermoElement c = new ThermoElement(n.NodeID);
            //    await c.InitializeAsync();
            //    networkElements.Add(c);
            //}
            //else if (proto.GenericType == GenericType.WallController)
            //{
            //    WallController c = new WallController(n.NodeID);
            //    await c.InitializeAsync();
            //    networkElements.Add(c);
            //}
            //else if (proto.GenericType == GenericType.SwitchBinary)
            //{
            //    WallPlug c = new WallPlug(n.NodeID);
            //    await c.InitializeAsync();
            //    networkElements.Add(c);
            //}
            //else if (proto.GenericType == GenericType.SensorMultiLevel)
            //{
            //    MultiSensor c = new MultiSensor(n.NodeID);
            //    await c.InitializeAsync();
            //    networkElements.Add(c);
            //}
            //else
            //{
            //    Logger.Instance.LogWarning("Initialize UnknownElement " + n.NodeID);
            //    UnknownElement unknown = new UnknownElement(n.NodeID);
            //    networkElements.Add(unknown);
            //}
            //}
            //catch (Exception ex)
            //{
            //    Logger.Instance.LogException("Initialize Node " + n.NodeID + ": ", ex);
            //    UnknownElement unknown = new UnknownElement(n.NodeID);
            //    networkElements.Add(unknown);
            //}
            //finally
            //{
            //    await Task.Delay(500);
            //}
        }
    }
}
