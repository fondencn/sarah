using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Repositories;
using Sarah.Messaging.RabbitMQ;
using Sarah.API.Interfaces.Services;
using Sarah.ServiceClients;
using Sarah.API.Interfaces;
using Sarah.API.Businessobjects;
using Sarah.ServiceDefaults;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure JWT Bearer Token Authentication with Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"))
        .ConfigureWarnings(warnings => warnings.Log(
            (RelationalEventId.CommandExecuting, LogLevel.Debug),
            (RelationalEventId.CommandExecuted, LogLevel.Debug))));

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));


// Register HTTP client for DeviceService communication
builder.Services.AddHttpClient<IDeviceService, DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["services__deviceservice__http__0"]
        ?? builder.Configuration["services__deviceservice__http-api__0"]
        ?? builder.Configuration["DeviceServiceUrl"]
        ?? "https+http://deviceservice";
    client.BaseAddress = new Uri(deviceServiceUrl);
})
.AddBearerTokenForwarding();

// Register DeviceServiceClient as concrete type for Rules actions
builder.Services.AddHttpClient<DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["services__deviceservice__http__0"]
        ?? builder.Configuration["services__deviceservice__http-api__0"]
        ?? builder.Configuration["DeviceServiceUrl"]
        ?? "https+http://deviceservice";
    client.BaseAddress = new Uri(deviceServiceUrl);
})
.AddBearerTokenForwarding();

// Register HTTP client for PersonService communication
builder.Services.AddHttpClient<IPersonService, PersonServiceClient>(client =>
{
    var personServiceUrl = builder.Configuration["services__personsservice__http__0"]
        ?? builder.Configuration["services__personsservice__http-api__0"]
        ?? builder.Configuration["PersonServiceUrl"]
        ?? "https+http://personsservice";
    client.BaseAddress = new Uri(personServiceUrl);
})
.AddBearerTokenForwarding();

// Register RabbitMQ client
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQClient>>();
    return new RabbitMQClient(logger, builder.Configuration);
});

// register helper application services
builder.Services.AddSingleton<IEmailNotifier, DieRooterEmailNotifier>();
builder.Services.AddSingleton<Sarah.Rules.HardCodedRuleStore>();

// Add RuleService as Singleton and then again the same instance as IHostedService and IRuleService
builder.Services.AddSingleton<Sarah.Rules.RuleService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<Sarah.Rules.RuleService>());
builder.Services.AddSingleton<Sarah.API.Interfaces.Services.IRuleService>(sp => sp.GetRequiredService<Sarah.Rules.RuleService>());

// Register MessageBasedWeatherProvider as IWeatherProvider and as IHostedService
builder.Services.AddSingleton<Sarah.Rules.Services.MessageBasedWeatherProvider>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Sarah.Rules.Services.MessageBasedWeatherProvider>());
builder.Services.AddSingleton<Sarah.API.Interfaces.IWeatherProvider>(sp => sp.GetRequiredService<Sarah.Rules.Services.MessageBasedWeatherProvider>());

// Register schedule services
builder.Services.AddScoped<Sarah.Rules.Services.AlarmScheduleService>();
builder.Services.AddScoped<Sarah.Rules.Services.TemperatureScheduleService>();

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Sarah Rules Service API", 
        Version = "v1",
        Description = "API for managing automation rules and conditions"
    });
    
    // Add JWT Authentication to Swagger
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

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sarah Rules Service API v1");
    });
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// register the hardcoded rule store    
var ruleSvc = app.Services.GetRequiredService<Sarah.Rules.RuleService>();
var hardCoded = app.Services.GetRequiredService<Sarah.Rules.HardCodedRuleStore>();

ruleSvc.RegisterRuleStore(hardCoded);

// Initialize AlarmScheduleService
using (var scope = app.Services.CreateScope())
{
    var alarmService = scope.ServiceProvider.GetRequiredService<Sarah.Rules.Services.AlarmScheduleService>();
    await alarmService.Start();
}

app.Run();
