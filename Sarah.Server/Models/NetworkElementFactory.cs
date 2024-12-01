using Sarah.Server.Models.Dtos;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Logging;

namespace Sarah.Server.Models;

public static class NetworkElementFactory
{
    /// <summary>
    /// Factory
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<NetworkElementDto> Create(IDeviceService deviceService, out string statusMessage)
    {
        statusMessage = null;
        List<NetworkElementDto> networkElements = new List<NetworkElementDto>();
        try
        {
            if (!String.IsNullOrWhiteSpace(deviceService.StatusMessage))
            {
                statusMessage = deviceService.StatusMessage;
            }
            /* Alle  Lampen aufzählen */
            foreach (ILamp lamp in deviceService.Lamps)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = lamp.NodeID,
                    TypeName = "Lampe",
                    Name = "Lampe (ID " + lamp.NodeID.ToString("000") + ")"
                };
                itemVm.Info =
                    "Helligkeit: " + lamp.Brightness.ToString() + Environment.NewLine +
                    "Farbe: " + lamp.Color;
                networkElements.Add(itemVm);
            }
            /* Alle BinarySensors aufzählen */
            foreach (IDoorSensor sensor in deviceService.DoorSensors)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = sensor.NodeID,
                    TypeName = "Türsensor",
                    Name = "Sensor (ID " + sensor.NodeID.ToString("000") + ")"
                };
                itemVm.Info = sensor.StateInfo;
                networkElements.Add(itemVm);
            }
            /* Alle Heizungen aufzählen */
            foreach (IThermoElement thermo in deviceService.Heatings)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = thermo.NodeID,
                    TypeName = "Thermo",
                    Name = "♨ Heizung (ID " + thermo.NodeID.ToString("000") + ")"
                };
                itemVm.Info = thermo.StateInfo;
                networkElements.Add(itemVm);
            }
            /* Alle Controllers aufzählen */
            foreach (IControllerElement controller in deviceService.Controllers)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = controller.NodeID,
                    TypeName = "Controller",
                    Name = "Controller (ID " + controller.NodeID.ToString("000") + ")"
                };
                itemVm.Info = "✨✨✨";
                networkElements.Add(itemVm);
            }
            foreach (IWallController controller in deviceService.WallControllers)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = controller.NodeID,
                    TypeName = "Wandschalter",
                    Name = "Wandschalter (ID " + controller.NodeID.ToString("000") + ")"
                };
                itemVm.Info = controller.StateInfo;
                networkElements.Add(itemVm);
            }
            foreach (IWallPlug wallplug in deviceService.WallPlugs)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = wallplug.NodeID,
                    TypeName = "Steckdose",
                    Name = "Steckdose (ID " + wallplug.NodeID.ToString("000") + ")"
                };
                itemVm.Info = wallplug.StateInfo;
                networkElements.Add(itemVm);
            }

            foreach (IMultiSensor sensor in deviceService.Sensors)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = sensor.NodeID,
                    TypeName = "Multisensor",
                    Name = "Steckdose (ID " + sensor.NodeID.ToString("000") + ")"
                };
                itemVm.Info = sensor.StateInfo;
                networkElements.Add(itemVm);
            }
            /* Alle unbekannten Elemente aufzählen */
            foreach (IUnknownElement unknown in deviceService.UnknownElements)
            {
                NetworkElementDto itemVm = new NetworkElementDto()
                {
                    ID = unknown.NodeID,
                    TypeName = "Unknown",
                    Name = "Unbekanntes Gerät (ID " + unknown.NodeID.ToString("000") + ")"
                };
                itemVm.Info = "❓";
                networkElements.Add(itemVm);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.LogDebug(ex.Message);
            networkElements.Add(new NetworkElementDto() { Name = "Fehler", Info = ex.Message });
        }

        return networkElements;
    }
}
