var builder = DistributedApplication.CreateBuilder(args);

// Add Keycloak IDP
var keycloak = builder.AddKeycloak("keycloak", port: 8443)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add RabbitMQ message broker
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add microservices as containers for now
// TODO: Once Aspire NuGet-only approach is clarified, 
// we can use AddProject for direct service integration

builder.Build().Run();
