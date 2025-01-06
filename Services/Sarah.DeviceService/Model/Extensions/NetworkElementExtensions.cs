using Sarah.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZWave;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;

namespace Sarah.DeviceService.Model.Extensions
{
    public static class NetworkElementExtensions
    {
        /// <summary>
        /// Entfernt den Knoten aus dem Netzwerk wenn er defekt ist
        /// </summary>
        /// <returns>Task</returns>
        public static async Task RemoveFailedNode(this NetworkElement el, IDeviceService deviceService)
        {
            Node n = deviceService.GetNode(el.NodeID) as Node;
            if (n != null)
            {
                await n.RemoveFailedNode();
                Logger.Instance.LogDebug("RemoveFailedNodee for Node " + el.NodeID);
            }
        }


        /// <summary>
        /// Aktualisiert die benachbarte Knoten Liste für diesen Knoten
        /// </summary>
        /// <returns>Task</returns>
        public static async Task<string> UpdateNeighbors(this NetworkElement el, IDeviceService deviceService)
        {
            Node n = deviceService.GetNode(el.NodeID) as Node;
            if (n != null)
            {
                NeighborUpdateStatus res = await n.RequestNeighborUpdate();
                Logger.Instance.LogDebug("Neighbor Update for Node " + el.NodeID + ": " + res);
                return res.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// "Heilung", genaue Funktion unbekannt
        /// </summary>
        /// <returns>Task</returns>
        public static async Task<string> HealNodeNetwork(this NetworkElement el, IDeviceService deviceService)
        {
            Node n = deviceService.GetNode(el.NodeID) as Node;
            if (n != null)
            {
                HealNetworkStatus res = await n.HealNodeNetwork();
                Logger.Instance.LogDebug("Healing Network for Node " + el.NodeID + ": " + res);
                return res.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Ermittelt alle Nachbarknoten zu diesem Knoten
        /// </summary>
        /// <returns>Knotenids der Nachabrknoten </returns>
        public static async Task<byte[]> GetNeighbors(this NetworkElement el, IDeviceService deviceService)
        {
            Node n = deviceService.GetNode(el.NodeID) as Node;
            if (n != null)
            {
                Node[] neighbors = await n.GetNeighbours();
                return neighbors.Select(item => item.NodeID).ToArray();
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
