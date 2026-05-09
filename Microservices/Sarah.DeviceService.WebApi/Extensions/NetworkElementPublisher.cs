using Sarah.API.BusinessObjects;
using Sarah.Messaging.RabbitMQ;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.DeviceService.WebApi.Extensions
{
    /// <summary>
    /// Publisher for NetworkElement events to RabbitMQ
    /// </summary>
    public class NetworkElementPublisher
    {
        private readonly RabbitMQClient _rabbitMQClient;

        public NetworkElementPublisher(RabbitMQClient rabbitMQClient)
        {
            _rabbitMQClient = rabbitMQClient ?? throw new ArgumentNullException(nameof(rabbitMQClient));
        }

        /// <summary>
        /// Reports a network event for this element by publishing a NetworkEventMessage to RabbitMQ
        /// </summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="element">The network element</param>
        /// <param name="propertyName">Name of the changed property</param>
        /// <param name="newValue">The new value</param>
        /// <returns>Task</returns>
        public async Task ReportEvent<TValue>(
            NetworkElement element,
            string propertyName,
            TValue? newValue)
        {
            var message = new NetworkEventMessage<TValue>
            {
                SourceNodeId = element.NodeID,
                Property = propertyName,
                NewValue = newValue
            };

            await _rabbitMQClient.PublishAsync(message);
        }

        /// <summary>
        /// Reports a network event without a value
        /// </summary>
        /// <param name="element">The network element</param>
        /// <param name="propertyName">Name of the changed property</param>
        /// <returns>Task</returns>
        public async Task ReportEvent(
            NetworkElement element,
            string propertyName)
        {
            await ReportEvent<object>(element, propertyName, null);
        }

        /// <summary>
        /// Publishes a typed clicked event so Rules can match scene IDs without calling GetNetworkItem.
        /// </summary>
        public async Task ReportClickedEvent(NetworkElement element, byte sceneId)
        {
            var message = new ClickedEventMessage(element.NodeID, sceneId);
            await _rabbitMQClient.PublishAsync(message);
        }

        /// <summary>
        /// Publishes a door-state-changed event on the generic network-events topic.
        /// </summary>
        public async Task ReportDoorStateChanged(NetworkElement element, bool isOpen)
        {
            var message = new NetworkEventMessage<bool>
            {
                SourceNodeId = element.NodeID,
                Property = "DoorState",
                NewValue = isOpen
            };
            await _rabbitMQClient.PublishAsync(message);
        }

        /// <summary>
        /// Publishes a tracker-button event so Rules can evaluate TrackerButtonPressedCondition.
        /// </summary>
        public async Task ReportTrackerButtonPressed(NetworkElement element, bool isPressed)
        {
            var message = new TrackerButtonPressedMessage(element.NodeID, isPressed);
            await _rabbitMQClient.PublishAsync(message);
        }

        /// <summary>
        /// Publishes a wall plug state event so Rules can evaluate WallPlug* conditions.
        /// </summary>
        public async Task ReportWallPlugStateChanged(NetworkElement element, bool isOn, DateTime lastChangeToPowerLow, DateTime lastChangeToPowerHigh)
        {
            var message = new WallPlugStateChangedMessage(element.NodeID, isOn, lastChangeToPowerLow, lastChangeToPowerHigh);
            await _rabbitMQClient.PublishAsync(message);
        }

        /// <summary>
        /// Publishes a multi-sensor state event so Rules can evaluate Presence/Luminance conditions.
        /// </summary>
        public async Task ReportMultiSensorStateChanged(NetworkElement element, float? presence, float? luminance)
        {
            var message = new MultiSensorStateChangedMessage(element.NodeID, presence, luminance);
            await _rabbitMQClient.PublishAsync(message);
        }

        /// <summary>
        /// Publishes a smoke sensor alert event so Rules can evaluate AlertCondition.
        /// </summary>
        public async Task ReportSmokeAlarm(NetworkElement element, bool alarmActive)
        {
            var message = new SmokeSensorAlertMessage(element.NodeID, alarmActive);
            await _rabbitMQClient.PublishAsync(message);
        }
    }
}
