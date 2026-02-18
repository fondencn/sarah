var builder = DistributedApplication.CreateBuilder(args);

// Add configuration for Keycloak admin credentials
var keycloakAdminUser = builder.Configuration["Keycloak:AdminUser"] ?? "admin";
var keycloakAdminPassword = builder.Configuration["Keycloak:AdminPassword"] ?? "admin";

// Add Keycloak IDP with realm import (HTTPS-only mode)
#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("KEYCLOAK_ADMIN", keycloakAdminUser)
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEnvironment("KC_HOSTNAME", "localhost")
    .WithArgs("--features=preview")
    .WithArgs("--spi-connections-http-client-default-disable-trust-manager=true")
    .WithHttpsDeveloperCertificate()
    .WithDeveloperCertificateTrust(true)
    .WithOtlpExporter()
    .WithRealmImport("./sarah-realm-realm.json");
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Add RabbitMQ message broker
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// Add single PostgreSQL instance with multiple databases
var postgres = builder.AddPostgres("postgres");
 
var postgresDevices = postgres.AddDatabase("devicesdb");
var postgresPersons = postgres.AddDatabase("personsdb");
var postgresMonitoring = postgres.AddDatabase("monitoringdb");
var postgresRules = postgres.AddDatabase("rulesdb");
var postgresRooms = postgres.AddDatabase("roomsdb");

// Add microservices with their dependencies
var deviceService = builder.AddProject<Projects.Sarah_DeviceService_WebApi>("deviceservice")
    .WithHttpsEndpoint(port: 5001, name: "https-api", env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresDevices, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WithDeveloperCertificateTrust(true)
    .WithEnvironment("ZWave__SerialPortName", builder.Configuration["ZWave:SerialPortName"] ?? "/dev/ttyUSB0")
    .WithEnvironment("TheThingsNetwork__ApiKey", builder.Configuration["TheThingsNetwork:ApiKey"] ?? "")
    .WaitFor(rabbitmq);

var personsService = builder.AddProject<Projects.Sarah_Persons_WebApi>("personsservice")
    .WithHttpsEndpoint(port: 5002, name: "https-api", env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresPersons, "PostgresConnection")
    .WithReference(keycloak)
    .WithDeveloperCertificateTrust(true)
    .WithReference(rabbitmq)
    .WithReference(deviceService)
    .WaitFor(rabbitmq);

var geofencesService = builder.AddProject<Projects.Sarah_Geofences_WebApi>("geofencesservice")
    .WithHttpsEndpoint(port: 5003, name: "https-api", env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(keycloak)
    .WithDeveloperCertificateTrust(true)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var roomService = builder.AddProject<Projects.Sarah_RoomService_WebApi>("roomservice")
    .WithHttpsEndpoint(port: 5004, name: "https-api", env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(keycloak)
    .WithDeveloperCertificateTrust(true)
    .WithReference(postgresRooms, "PostgresConnection")
    .WithReference(keycloak);

var monitoringService = builder.AddProject<Projects.Sarah_Monitoring_WebApi>("monitoringservice")
    .WithHttpsEndpoint(port: 5005, name: "https-api", env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresMonitoring, "PostgresConnection")
    .WithReference(keycloak)
    .WithDeveloperCertificateTrust(true)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var rulesService = builder.AddProject<Projects.Sarah_Rules_WebApi>("rulesservice")
    .WithHttpsEndpoint(port: 5006, name: "https-api", env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresRules, "PostgresConnection")
    .WithReference(keycloak)
    .WithDeveloperCertificateTrust(true)
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
    .WithHttpsEndpoint(port: 5008, name: "https-api", env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(keycloak)
    .WithDeveloperCertificateTrust(true)
    .WithReference(rabbitmq)
    .WithReference(deviceService)
    .WaitFor(rabbitmq);

// Add frontend (Angular client)
var frontend = builder.AddJavaScriptApp("frontend", "../sarah.client")
    .WithNpm()
    .WithDeveloperCertificateTrust(true)
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
    frontend.WithExplicitStart();
}

builder.Build().Run();

