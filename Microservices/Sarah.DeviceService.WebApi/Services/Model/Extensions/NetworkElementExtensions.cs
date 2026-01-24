using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZWave;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces.Services;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.DeviceService.Model.Extensions
{
    public static class NetworkElementExtensions
    {
        /// <summary>
        /// Reports a network event for this element by publishing a NetworkEventMessage to RabbitMQ
        /// </summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="element">The network element</param>
        /// <param name="rabbitMQ">RabbitMQ client instance</param>
        /// <param name="propertyName">Name of the changed property</param>
        /// <param name="newValue">The new value</param>
        /// <returns>Task</returns>
        public static async Task ReportEvent<TValue>(
            this NetworkElement element, 
            RabbitMQClient rabbitMQ, 
            string propertyName, 
            TValue? newValue)
        {
            var message = new NetworkEventMessage<TValue>
            {
                SourceNodeId = element.NodeID,
                Property = propertyName,
                NewValue = newValue
            };

            await rabbitMQ.PublishAsync(message);
        }

        /// <summary>
        /// Reports a network event without a value
        /// </summary>
        /// <param name="element">The network element</param>
        /// <param name="rabbitMQ">RabbitMQ client instance</param>
        /// <param name="propertyName">Name of the changed property</param>
        /// <returns>Task</returns>
        public static async Task ReportEvent(
            this NetworkElement element, 
            RabbitMQClient rabbitMQ, 
            string propertyName)
        {
            await element.ReportEvent<object>(rabbitMQ, propertyName, null);
        }


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
                // Console.WriteLine("RemoveFailedNodee for Node " + el.NodeID);
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
                // Console.WriteLine("Neighbor Update for Node " + el.NodeID + ": " + res);
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
                // Console.WriteLine("Healing Network for Node " + el.NodeID + ": " + res);
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
