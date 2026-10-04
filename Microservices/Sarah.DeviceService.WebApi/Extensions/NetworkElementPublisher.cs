using Sarah.API.BusinessObjects;
using Sarah.DeviceService.Model;
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
        private readonly EnergyMeasurementThrottle _energyThrottle = new();

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
        public virtual async Task ReportEvent<TValue>(
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
        public virtual async Task ReportEvent(
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

        /// <summary>
        /// Publishes a (throttled) energy measurement so the Statistics service can store consumption time series.
        /// </summary>
        public virtual async Task ReportEnergyMeasured(WallPlug wallPlug, double powerW, double? energyKwhTotal)
        {
            var now = DateTime.UtcNow;
            if (!_energyThrottle.ShouldPublish(wallPlug.NodeID, powerW, now))
            {
                return;
            }

            var message = new DeviceEnergyMeasuredMessage
            {
                DeviceId = wallPlug.NodeID,
                DeviceName = wallPlug.Name,
                MeasuredAt = now,
                PowerW = powerW,
                EnergyKwhTotal = energyKwhTotal
            };
            await _rabbitMQClient.PublishAsync(message);
        }

        internal async Task  ReportWallPlugEnabledChanged(WallPlug wallPlug)
        {
            var message = new WallPlugEnabledChangedMessage(wallPlug.IsOn)
            {
                SourceNodeId = wallPlug.NodeID
            };
            await _rabbitMQClient.PublishAsync(message);
        }

        internal async Task ReportWallPlugPowerLow(WallPlug wallPlug)
        {
            var message = new WallPlugPowerLowMessage
            {
                SourceNodeId = wallPlug.NodeID
            };
            await _rabbitMQClient.PublishAsync(message);
        }

        internal async Task ReportWallPlugPowerHigh(WallPlug wallPlug)
        {
            var message = new WallPlugPowerHighMessage
            {
                SourceNodeId = wallPlug.NodeID
            };
            await _rabbitMQClient.PublishAsync(message);
        }
    }
}
