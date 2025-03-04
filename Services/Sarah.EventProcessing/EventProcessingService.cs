using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Sarah.API.Interfaces;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Sarah.API.BusinessObjects;

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
            { typeof(NetworkEvent), "networkevents" }
        };  

        public EventProcessingService(ILogger<EventProcessingService> logger, IConfiguration configuration)    
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InitializeAsync()
        {
            var factory = new ConnectionFactory() { HostName = _configuration["RabbitMQ:HostName"] };
            
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();


            foreach(var queueName in _exchangeNames.Values)
            {
                await _channel.ExchangeDeclareAsync(exchange: queueName, type: ExchangeType.Direct);
                await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                await _channel.QueueBindAsync(queue: queueName, exchange: queueName, routingKey: "");    
            }
      
        }

        public  Task PublishNetworkEventAsync(NetworkEvent networkEvent, CancellationToken cancellationToken = default)
        {
            string queueName = _exchangeNames[networkEvent.GetType()];
            string message = JsonSerializer.Serialize(networkEvent);
            return PublishEvent(queueName, message, cancellationToken);
        }

        private async Task PublishEvent(string queueName, string message, CancellationToken cancellationToken = default)
        {
            var body = Encoding.UTF8.GetBytes(message);


            await _channel.BasicPublishAsync(
                exchange: queueName, 
                routingKey: "",
                mandatory: true,  
                body: body,
                cancellationToken);

            _logger.LogInformation("{1} Sent to {0}", queueName, message);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
