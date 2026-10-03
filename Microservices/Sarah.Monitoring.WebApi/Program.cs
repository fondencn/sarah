using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Sarah.Messaging.RabbitMQ;
using Sarah.API.Interfaces.Services;
using Sarah.Monitoring;
using Sarah.Monitoring.Clients;
using Sarah.ServiceClients;
using Sarah.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure JWT Bearer Token Authentication with Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// Register client credentials handler for background service → service calls (no HTTP context to forward from)
builder.Services.AddTransient<ClientCredentialsHandler>();

// Register HTTP client for Device Service (via interface, for event-driven calls)
builder.Services.AddHttpClient<IDeviceService, DeviceServiceClient>(client =>
{
    var url = builder.Configuration["services__deviceservice__http__0"]
        ?? builder.Configuration["services__deviceservice__http-api__0"]
        ?? builder.Configuration["DeviceServiceUrl"]
        ?? "https+http://deviceservice";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<ClientCredentialsHandler>();

// Register HTTP client for Device Service (concrete type, for snapshot fetching at startup)
builder.Services.AddHttpClient<DeviceServiceClient>(client =>
{
    var url = builder.Configuration["services__deviceservice__http__0"]
        ?? builder.Configuration["services__deviceservice__http-api__0"]
        ?? builder.Configuration["DeviceServiceUrl"]
        ?? "https+http://deviceservice";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<ClientCredentialsHandler>();

// Register HTTP client for Person Service
builder.Services.AddHttpClient<IPersonService, PersonServiceClient>(client =>
{
    var url = builder.Configuration["services__personsservice__http__0"]
        ?? builder.Configuration["services__personsservice__http-api__0"]
        ?? builder.Configuration["PersonServiceUrl"]
        ?? "https+http://personsservice";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<ClientCredentialsHandler>();

// Register HTTP client for Room Service
builder.Services.AddHttpClient<RoomServiceClient>(client =>
{
    var url = builder.Configuration["services__roomservice__http__0"]
        ?? builder.Configuration["services__roomservice__http-api__0"]
        ?? builder.Configuration["RoomServiceUrl"]
        ?? "https+http://roomservice";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<ClientCredentialsHandler>();

// Register HTTP client for StromGedacht OpenAPI
builder.Services.AddHttpClient<StromGedachtNowApiClient>(client =>
{
    var baseUrl = builder.Configuration["StromGedacht:BaseUrl"]
        ?? "https://api.stromgedacht.de";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Register RabbitMQ client
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQClient>>();
    return new RabbitMQClient(logger, builder.Configuration);
});

// Register MonitoringService as a hosted background service
builder.Services.AddHostedService<Sarah.Monitoring.MonitoringService>();
builder.Services.AddSingleton<MonitoringService>(sp =>
    sp.GetServices<IHostedService>().OfType<Sarah.Monitoring.MonitoringService>().First());

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sarah Monitoring Service API",
        Version = "v1",
        Description = "API for monitoring weather, vacation schedules, and other environmental data"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sarah Monitoring Service API v1");
    });
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

