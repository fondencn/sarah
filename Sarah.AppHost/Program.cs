var builder = DistributedApplication.CreateBuilder(args);

// Add configuration for Keycloak admin credentials
var keycloakAdminUser = builder.Configuration["Keycloak:AdminUser"] ?? "admin";
var keycloakAdminPassword = builder.Configuration["Keycloak:AdminPassword"] ?? "admin";

var postgresUserName = builder.AddParameter(
    "postgres-username",
    builder.Configuration["Postgres:Username"] ?? "postgres",
    publishValueAsDefault: true,
    secret: false);

var postgresPassword = builder.AddParameter(
    "postgres-password",
    builder.Configuration["Postgres:Password"] ?? "postgres",
    publishValueAsDefault: false,
    secret: true);

// Add Keycloak IDP with realm import (HTTP-only mode)
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint("management", endpoint => endpoint.UriScheme = "http")
    .WithEnvironment("KEYCLOAK_ADMIN", keycloakAdminUser)
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HTTP_MANAGEMENT_SCHEME", "http")
    .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEnvironment("KC_HOSTNAME", "localhost")
    .WithArgs("--features=preview")
    .WithArgs("--spi-connections-http-client-default-disable-trust-manager=true")
    .WithOtlpExporter()
    .WithRealmImport("./sarah-realm-realm.json");

keycloak.OnResourceEndpointsAllocated((_, _, _) =>
{
    keycloak.WithEndpoint("management", endpoint => endpoint.UriScheme = "http");
    return Task.CompletedTask;
});

// Add RabbitMQ message broker
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// Add single PostgreSQL instance with multiple databases
var postgres = builder.AddPostgres("postgres", postgresUserName, postgresPassword)
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
 
var postgresDevices = postgres.AddDatabase("devicesdb");
var postgresPersons = postgres.AddDatabase("personsdb");
var postgresMonitoring = postgres.AddDatabase("monitoringdb");
var postgresRules = postgres.AddDatabase("rulesdb");
var postgresRooms = postgres.AddDatabase("roomsdb");
var postgresDashboard = postgres.AddDatabase("dashboarddb");

// Add microservices with their dependencies
var deviceService = builder.AddProject<Projects.Sarah_DeviceService_WebApi>("deviceservice")
    .WithHttpEndpoint(port: 5001, name: "http-api")
    .WithReference(postgresDevices, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WithEnvironment("ZWave__SerialPortName", builder.Configuration["ZWave:SerialPortName"] ?? "/dev/ttyUSB0")
    .WithEnvironment("TheThingsNetwork__ApiKey", builder.Configuration["TheThingsNetwork:ApiKey"] ?? "")
    .WaitFor(rabbitmq);

var geofencesService = builder.AddProject<Projects.Sarah_Geofences_WebApi>("geofencesservice")
    .WithHttpEndpoint(port: 5003, name: "http-api")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var personsService = builder.AddProject<Projects.Sarah_Persons_WebApi>("personsservice")
    .WithHttpEndpoint(port: 5002, name: "http-api")
    .WithReference(postgresPersons, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WithReference(deviceService)
    .WithReference(geofencesService)
    .WaitFor(rabbitmq);

var roomService = builder.AddProject<Projects.Sarah_RoomService_WebApi>("roomservice")
    .WithHttpEndpoint(port: 5004, name: "http-api")
    .WithReference(keycloak)
    .WithReference(postgresRooms, "PostgresConnection")
    .WithReference(keycloak);

var monitoringService = builder.AddProject<Projects.Sarah_Monitoring_WebApi>("monitoringservice")
    .WithHttpEndpoint(port: 5005, name: "http-api")
    .WithReference(postgresMonitoring, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var rulesService = builder.AddProject<Projects.Sarah_Rules_WebApi>("rulesservice")
    .WithHttpEndpoint(port: 5006, name: "http-api")
    .WithReference(postgresRules, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WithReference(deviceService)
    .WithReference(personsService)
    .WithEnvironment("EmailNotifier__SmtpSender", builder.Configuration["EmailNotifier:SmtpSender"] ?? "")
    .WithEnvironment("EmailNotifier__SmtpServer", builder.Configuration["EmailNotifier:SmtpServer"] ?? "")
    .WithEnvironment("EmailNotifier__SmtpPort", builder.Configuration["EmailNotifier:SmtpPort"] ?? "25")
    .WithEnvironment("EmailNotifier__SmtpUsername", builder.Configuration["EmailNotifier:SmtpUsername"] ?? "")
    .WithEnvironment("EmailNotifier__SmtpPassword", builder.Configuration["EmailNotifier:SmtpPassword"] ?? "")
    .WaitFor(rabbitmq);

var speechServer = builder.AddProject<Projects.Sarah_SpeechServer_WebApi>("speechserver")
    .WithHttpEndpoint(port: 5008, name: "http-api")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WithReference(deviceService)
    .WaitFor(rabbitmq);

var dashboardService = builder.AddProject<Projects.Sarah_Dashboard_WebApi>("dashboardservice")
    .WithHttpEndpoint(port: 5007, name: "http-api")
    .WithReference(postgresDashboard, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(deviceService)
    .WithReference(personsService);

// Add frontend (Angular client)
var frontend = builder.AddJavaScriptApp("frontend", "../sarah.client")
    .WithNpm()
    .WithReference(keycloak)
    .WithRunScript("start");

if (builder.Environment.IsDevelopment())
{
    deviceService.WithExplicitStart();
    personsService.WithExplicitStart();
    geofencesService.WithExplicitStart();
    roomService.WithExplicitStart();
    monitoringService.WithExplicitStart();
    rulesService.WithExplicitStart();
    speechServer.WithExplicitStart();
    dashboardService.WithExplicitStart();
    frontend.WithExplicitStart();
}

builder.Build().Run();

