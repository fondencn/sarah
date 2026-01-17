var builder = DistributedApplication.CreateBuilder(args);

// Add Keycloak IDP
var keycloak = builder.AddKeycloak("keycloak", 8443)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add RabbitMQ message broker
var rabbitmq = builder.AddRabbitMQ("rabbitmq",
              userName: builder.AddParameter("username", "guest", secret: true),
              password: builder.AddParameter("password", "guest", secret: true))
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsConnectionString();

// Add single PostgreSQL instance with multiple databases
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDevices = postgres.AddDatabase("devicesdb");
var postgresPersons = postgres.AddDatabase("personsdb");
var postgresGeofences = postgres.AddDatabase("geofencesdb");
var postgresEvents = postgres.AddDatabase("eventsdb");
var postgresMonitoring = postgres.AddDatabase("monitoringdb");
var postgresRules = postgres.AddDatabase("rulesdb");

// Add microservices with their dependencies
var deviceService = builder.AddProject<Projects.Sarah_DeviceService_WebApi>("deviceservice")
    .WithHttpsEndpoint(port: 5001, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresDevices, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

var personsService = builder.AddProject<Projects.Sarah_Persons_WebApi>("personsservice")
    .WithHttpsEndpoint(port: 5002, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresPersons, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

var geofencesService = builder.AddProject<Projects.Sarah_Geofences_WebApi>("geofencesservice")
    .WithHttpsEndpoint(port: 5003, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresGeofences, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

var eventProcessingService = builder.AddProject<Projects.Sarah_EventProcessing_WebApi>("eventprocessing")
    .WithHttpsEndpoint(port: 5004, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresEvents, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

var monitoringService = builder.AddProject<Projects.Sarah_Monitoring_WebApi>("monitoringservice")
    .WithHttpsEndpoint(port: 5005, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresMonitoring, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

var rulesService = builder.AddProject<Projects.Sarah_Rules_WebApi>("rulesservice")
    .WithHttpsEndpoint(port: 5006, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresRules, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

// Add LocationServer and SpeechServer (may not need separate databases)
var locationServer = builder.AddProject<Projects.Sarah_LocationServer_WebApi>("locationserver")
    .WithHttpsEndpoint(port: 5007, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

var speechServer = builder.AddProject<Projects.Sarah_SpeechServer_WebApi>("speechserver")
    .WithHttpsEndpoint(port: 5008, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(keycloak)
    .WithReference(rabbitmq);

// Add frontend (Angular client)
var frontend = builder.AddProject<Projects.sarah_client>("frontend")
    .WithHttpsEndpoint(port: 4200)
    .WithExternalHttpEndpoints();

builder.Build().Run();

