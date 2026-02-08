var builder = DistributedApplication.CreateBuilder(args);

// Add configuration for Keycloak admin credentials
var keycloakAdminUser = builder.Configuration["Keycloak:AdminUser"] ?? "admin";
var keycloakAdminPassword = builder.Configuration["Keycloak:AdminPassword"] ?? "admin";

// Add Keycloak IDP with realm import
var keycloak = builder.AddKeycloak("keycloak", 8443)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("KEYCLOAK_ADMIN", keycloakAdminUser)
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithBindMount("./keycloak-realm.json", "/opt/keycloak/data/import/realm.json")
    .WithArgs("start-dev", "--import-realm");

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
    .WithHttpsEndpoint(port: 5001, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresDevices, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var personsService = builder.AddProject<Projects.Sarah_Persons_WebApi>("personsservice")
    .WithHttpsEndpoint(port: 5002, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresPersons, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WithReference(deviceService)
    .WaitFor(rabbitmq);

var geofencesService = builder.AddProject<Projects.Sarah_Geofences_WebApi>("geofencesservice")
    .WithHttpsEndpoint(port: 5003, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var roomService = builder.AddProject<Projects.Sarah_RoomService_WebApi>("roomservice")
    .WithHttpsEndpoint(port: 5004, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresRooms, "PostgresConnection")
    .WithReference(keycloak);

var monitoringService = builder.AddProject<Projects.Sarah_Monitoring_WebApi>("monitoringservice")
    .WithHttpsEndpoint(port: 5005, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(postgresMonitoring, "PostgresConnection")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var rulesService = builder.AddProject<Projects.Sarah_Rules_WebApi>("rulesservice")
    .WithHttpsEndpoint(port: 5006, env: "ASPNETCORE_HTTPS_PORT")
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
    .WithHttpsEndpoint(port: 5008, env: "ASPNETCORE_HTTPS_PORT")
    .WithReference(keycloak)
    .WithReference(rabbitmq)
    .WithReference(deviceService)
    .WaitFor(rabbitmq);

// Add frontend (Angular client)
var frontend = builder.AddJavaScriptApp("frontend", "../sarah.client")
    .WithNpm()
    .WithRunScript("start");

builder.Build().Run();

