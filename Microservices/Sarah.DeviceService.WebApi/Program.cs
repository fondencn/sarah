using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Sarah.DeviceService.WebApi.Data;
using Sarah.DeviceService.WebApi.Data.Repositories;
using Sarah.Messaging.RabbitMQ;
using Sarah.DeviceService.WebApi.Extensions;
using Sarah.API.Interfaces.Services;
using Sarah.API.Interfaces;
using Sarah.DeviceService.WebApi.Services;
using System.Text.RegularExpressions;
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

// Register RabbitMQ client
builder.Services.AddSingleton<RabbitMQClient>();

// Register NetworkElementPublisher
builder.Services.AddSingleton<NetworkElementPublisher>();

// Register EventProcessingService with RabbitMQ
builder.Services.AddSingleton<IEventProcessingService, EventProcessingService>();

// Register NodeFactory
builder.Services.AddSingleton<INodeFactory, NodeFactory>();

// Register DeviceService as both a hosted service (BackgroundService) and as IDeviceService for DI
builder.Services.AddSingleton<Sarah.DeviceService.DeviceService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Sarah.DeviceService.DeviceService>());
builder.Services.AddSingleton<IDeviceService>(sp => sp.GetRequiredService<Sarah.DeviceService.DeviceService>());

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Sarah Device Service API", 
        Version = "v1",
        Description = "API for managing smart home devices (lamps, sensors, door sensors, wall plugs, thermostats)"
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

    c.CustomOperationIds(apiDesc =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"];
        var action = apiDesc.ActionDescriptor.RouteValues["action"];
        var method = apiDesc.HttpMethod ?? "Unknown";
        var relativePath = apiDesc.RelativePath ?? string.Empty;

        var sanitizedPath = Regex
            .Replace(relativePath, "[^a-zA-Z0-9]+", "_")
            .Trim('_');

        if (!string.IsNullOrWhiteSpace(controller) && !string.IsNullOrWhiteSpace(action))
        {
            return $"{controller}_{action}_{method}_{sanitizedPath}";
        }

        return null;
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sarah Device Service API v1");
    });
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
