using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Sarah.API.Interfaces;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Sarah.API.BusinessObjects;
using RabbitMQ.Client.Events;

namespace Sarah.EventProcessing
{
    public class EventProcessingService : IEventProcessingService, IDisposable
    {
        private readonly ILogger<EventProcessingService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly IConfiguration _configuration;

        private Dictionary<Type, string> _exchangeNames = new Dictionary<Type, string>
        {
            { typeof(NetworkEvent<string>),                 "networkevents-string" },
            { typeof(NetworkEvent<float>),                  "networkevents-float" },
            { typeof(NetworkEvent<bool>),                   "networkevents-bool" },
            { typeof(NetworkEvent<int>),                    "networkevents-int" },
            { typeof(NetworkEvent),                         "networkevents" },
            { typeof(AirQualityChangedEvent),               "airqualityevents" },
            { typeof(SayEvent),                             "speechevents" },
            { typeof(PersonAvailabilityEvent),              "personavailabilityevents" },
            { typeof(PersonGeoFenceEvent),                  "geofenceevents" },
            { typeof(OutDoorTemperatureChangedEvent),       "outdoortempevents" },
            { typeof(WeatherWarningEvent),                  "weatherwarningevents" },
        };  

        public EventProcessingService(ILogger<EventProcessingService> logger, IConfiguration configuration)    
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Start()
        {
            var factory = new ConnectionFactory() 
            { 
                HostName = _configuration["RabbitMQ:HostName"] ?? "",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "",
                Password = _configuration["RabbitMQ:Password"] ?? ""
            };
            
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();


            foreach(var queueName in _exchangeNames.Values)
            {
                await _channel.ExchangeDeclareAsync(exchange: queueName, type: ExchangeType.Direct);
                await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: true, arguments: null);
                await _channel.QueueBindAsync(queue: queueName, exchange: queueName, routingKey: "");    
            }
      
        }

        public  Task PublishNetworkEventAsync(NetworkEvent networkEvent, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[networkEvent.GetType()];
            string message = JsonSerializer.Serialize(networkEvent);
            return PublishEvent(queueName, message, cancellationToken);
        }

        public  Task PublishNetworkEventAsync<T>(NetworkEvent<T> networkEvent, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[networkEvent.GetType()];
            string message = JsonSerializer.Serialize(networkEvent);
            return PublishEvent(queueName, message, cancellationToken);
        }

        public async Task SubscribeNetworkEventAsync(INetworkEventSubscriber subscriber, CancellationToken cancellationToken = default)
        {
            var queueName = _exchangeNames[typeof(NetworkEvent<string>)];
            var consumer = new AsyncEventingBasicConsumer(_channel!);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventType = _exchangeNames.FirstOrDefault(x => x.Value == queueName).Key;
                var networkEvent = (NetworkEvent)JsonSerializer.Deserialize(message, eventType)!;

                await subscriber.Notify(networkEvent);
            };

            await _channel!.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumer: consumer,
                cancellationToken: cancellationToken);
            
            _logger.LogDebug($"{subscriber.GetType().Name} subscribed to queue {queueName}...");
        }

        public Task PublishAirQualityEventAsync(AirQualityChangedEvent airQualityChangedEvent, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[airQualityChangedEvent.GetType()];
            string message = JsonSerializer.Serialize(airQualityChangedEvent);
            return PublishEvent(queueName, message, cancellationToken);
        }

        public Task PublishSay(SayEvent e, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[e.GetType()];
            string message = JsonSerializer.Serialize(e);
            return PublishEvent(queueName, message, cancellationToken);
        }

        public Task PublishGeoFenceEventAsync(PersonGeoFenceEvent e, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[e.GetType()];
            string message = JsonSerializer.Serialize(e);
            return PublishEvent(queueName, message, cancellationToken);
        }

        public Task PublishPersonAvailabilityAsync(PersonAvailabilityEvent e, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[e.GetType()];
            string message = JsonSerializer.Serialize(e);
            return PublishEvent(queueName, message, cancellationToken);
        }



        public Task PublishOutDoorTemperatureChangedEventAsync(OutDoorTemperatureChangedEvent e, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[e.GetType()];
            string message = JsonSerializer.Serialize(e);
            return PublishEvent(queueName, message, cancellationToken);
        }

        public Task PublishWeatherWarningEventAsync(WeatherWarningEvent e, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[e.GetType()];
            string message = JsonSerializer.Serialize(e);
            return PublishEvent(queueName, message, cancellationToken);
        }

        private async Task PublishEvent(string queueName, string message, CancellationToken cancellationToken = default)
        {
            var body = Encoding.UTF8.GetBytes(message);

            if (_channel != null) 
            {
                await _channel.BasicPublishAsync(
                    exchange: queueName, 
                    routingKey: "",
                    mandatory: true,  
                    body: body,
                    cancellationToken);

                _logger.LogInformation($"Message {message} was sent to queue {queueName}...");
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
