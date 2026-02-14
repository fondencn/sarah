using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Sarah.Monitoring.WebApi.Data;
using Sarah.Monitoring.WebApi.Data.Repositories;
using Sarah.Messaging.RabbitMQ;
using Sarah.API.Interfaces.Services;
using Sarah.Monitoring;
using Sarah.ServiceClients;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Bearer Token Authentication with Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register HTTP client for Rules Service communication
// builder.Services.AddHttpClient<IRuleService, RulesServiceClient>(client =>
// {
//     var rulesServiceUrl = builder.Configuration["RulesServiceUrl"] ?? "http://localhost:5006";
//     client.BaseAddress = new Uri(rulesServiceUrl);
//     client.Timeout = TimeSpan.FromSeconds(30);
// });

// Register HTTP client for Device Service communication
builder.Services.AddHttpClient<IDeviceService, DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["DeviceServiceUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(deviceServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sarah Monitoring Service API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
