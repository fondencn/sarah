using Microsoft.Extensions.DependencyInjection;

namespace Sarah.Messaging.RabbitMQ;

/// <summary>
/// Extension methods for registering RabbitMQ messaging services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ messaging services to the service collection
    /// </summary>
    public static IServiceCollection AddRabbitMQMessaging(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMQClient>();
        return services;
    }
}
