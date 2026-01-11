var builder = DistributedApplication.CreateBuilder(args);

// Add Keycloak IDP

var keycloak = builder.AddKeycloak("keycloak", 8443)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add RabbitMQ message broker
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add PostgreSQL databases (one per microservice)
var postgresDevices = builder.AddPostgres("postgres-devices")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("devicesdb");

var postgresPersons = builder.AddPostgres("postgres-persons")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("personsdb");

var postgresGeofences = builder.AddPostgres("postgres-geofences")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("geofencesdb");

var postgresEvents = builder.AddPostgres("postgres-events")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("eventsdb");

var postgresMonitoring = builder.AddPostgres("postgres-monitoring")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("monitoringdb");

var postgresRules = builder.AddPostgres("postgres-rules")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("rulesdb");

// Note: Microservice projects would be added here when using Aspire with project references
// Example (commented out until projects are configured for Aspire):
// var deviceService = builder.AddProject<Projects.Sarah_DeviceService_WebApi>("deviceservice")
//     .WithReference(postgresDevices)
//     .WithReference(keycloak)
//     .WithReference(rabbitmq);

builder.Build().Run();
