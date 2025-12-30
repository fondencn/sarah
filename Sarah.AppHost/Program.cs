var builder = DistributedApplication.CreateBuilder(args);

// Add Keycloak IDP
var keycloak = builder.AddKeycloak("keycloak", port: 8443)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var keycloakRealm = "sarah-realm";

// Add RabbitMQ message broker
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add microservices
var deviceService = builder.AddProject<Projects.Sarah_DeviceService_WebApi>("deviceservice")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

var personsService = builder.AddProject<Projects.Sarah_Persons_WebApi>("personsservice")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

var geofencesService = builder.AddProject<Projects.Sarah_Geofences_WebApi>("geofencesservice")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

var eventProcessingService = builder.AddProject<Projects.Sarah_EventProcessing_WebApi>("eventprocessingservice")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

var monitoringService = builder.AddProject<Projects.Sarah_Monitoring_WebApi>("monitoringservice")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

var rulesService = builder.AddProject<Projects.Sarah_Rules_WebApi>("rulesservice")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

var apiGateway = builder.AddProject<Projects.Sarah_API_WebApi>("apigateway")
    .WithReference(rabbitmq)
    .WithReference(keycloak)
    .WithReference(deviceService)
    .WithReference(personsService)
    .WithReference(geofencesService)
    .WithReference(eventProcessingService)
    .WithReference(monitoringService)
    .WithReference(rulesService);

// Add existing servers
var locationServer = builder.AddProject<Projects.Sarah_LocationServer>("locationserver")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

var speechServer = builder.AddProject<Projects.Sarah_SpeechServer>("speechserver")
    .WithReference(rabbitmq)
    .WithReference(keycloak);

// Add Angular frontend
var frontend = builder.AddNpmApp("frontend", "../sarah.client")
    .WithReference(apiGateway)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
