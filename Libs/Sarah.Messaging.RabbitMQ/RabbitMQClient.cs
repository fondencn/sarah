using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Sarah.Messaging.RabbitMQ;

/// <summary>
/// Generic RabbitMQ messaging client for publishing and subscribing to topics
/// </summary>
public class RabbitMQClient : IDisposable
{
    private readonly ILogger<RabbitMQClient> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    private bool _isConnected = false;

    public RabbitMQClient(ILogger<RabbitMQClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Initialize the RabbitMQ connection
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if(this._isConnected)
        {
            _logger.LogDebug("RabbitMQClient is already connected.");
            return;
        }
        var factory = new ConnectionFactory();
        
        // Check if Aspire connection string is provided
        var connectionString = _configuration.GetConnectionString("rabbitmq");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Use Aspire-provided connection string (format: amqp://username:password@hostname:port)
            factory.Uri = new Uri(connectionString);
            _logger.LogInformation("Using Aspire RabbitMQ connection string");
        }
        else
        {
            // Fall back to individual configuration values
            factory.HostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
            factory.Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672");
            factory.UserName = _configuration["RabbitMQ:UserName"] ?? "guest";
            factory.Password = _configuration["RabbitMQ:Password"] ?? "guest";
            _logger.LogInformation("Using RabbitMQ configuration from appsettings");
        }

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync();
        _isConnected = true;
        _logger.LogInformation("Connected to RabbitMQ at {HostName}:{Port}", 
            factory.HostName, factory.Port);
    }

    /// <summary>
    /// Publish a message to a topic
    /// </summary>
    /// <typeparam name="T">Type of message (must inherit from AbstractMessage)</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="exchange">The exchange name (defaults to message topic)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishAsync<T>(T message, string? exchange = null, CancellationToken cancellationToken = default) 
        where T : AbstractMessage
    {
        await this.ConnectAsync(cancellationToken);

        if (_channel == null)
        {
            throw new InvalidOperationException("Not connected. Retry later.");
        }

        var exchangeName = exchange ?? message.Topic;
        
        // Declare exchange as topic type
        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: cancellationToken);

        // Serialize message to JSON
        var json = JsonSerializer.Serialize(message, message.GetType());
        var body = Encoding.UTF8.GetBytes(json);

        // Publish with the message's topic as routing key
        await _channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: message.Topic,
            mandatory: false,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug("Published message {MessageId} to topic {Topic} on exchange {Exchange}",
            message.MessageId, message.Topic, exchangeName);
    }

    /// <summary>
    /// Subscribe to messages on a topic
    /// </summary>
    /// <typeparam name="T">Type of message to receive (must inherit from AbstractMessage)</typeparam>
    /// <param name="topic">The topic pattern to subscribe to (supports wildcards: * for one word, # for zero or more words)</param>
    /// <param name="onMessage">Callback to handle received messages</param>
    /// <param name="exchange">The exchange name (defaults to topic)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the subscription</returns>
    public async Task SubscribeAsync<T>(
        string topic,
        Func<T, Task> onMessage,
        string? exchange = null,
        CancellationToken cancellationToken = default) 
        where T : AbstractMessage
    {
        if (typeof(T).IsAbstract || typeof(T).IsInterface)
        {
            throw new InvalidOperationException(
                $"SubscribeAsync requires a concrete message type. '{typeof(T).FullName}' is not supported.");
        }

        await this.ConnectAsync(cancellationToken);

        if (_channel == null)
        {
            throw new InvalidOperationException("Not connected. Retry later.");
        }


        var exchangeName = exchange ?? topic;

        // Declare exchange as topic type
        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: cancellationToken);

        // Declare a queue (auto-generated name)
        var queueDeclareResult = await _channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null,
            cancellationToken: cancellationToken);

        var queueName = queueDeclareResult.QueueName;

        // Bind queue to exchange with routing key pattern
        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: topic,
            arguments: null,
            cancellationToken: cancellationToken);

        // Create consumer
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                
                // Deserialize to specific message type
                var message = JsonSerializer.Deserialize<T>(json);
                
                if (message != null)
                {
                    await onMessage(message);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize message from topic {Topic}", topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Subscribed to topic {Topic} on exchange {Exchange}", topic, exchangeName);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _connection?.Dispose();
        
        _disposed = true;
        _isConnected = false;
        GC.SuppressFinalize(this);
    }
}
