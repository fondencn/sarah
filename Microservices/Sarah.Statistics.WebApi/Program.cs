using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sarah.Messaging.RabbitMQ;
using Sarah.Statistics.WebApi.Data;
using Sarah.Statistics.WebApi.Data.Repositories;
using Sarah.Statistics.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure JWT Bearer Token Authentication with Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<StatisticsDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"))
        .ConfigureWarnings(warnings => warnings.Log(
            (RelationalEventId.CommandExecuting, LogLevel.Debug),
            (RelationalEventId.CommandExecuted, LogLevel.Debug))));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register RabbitMQ client
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQClient>>();
    return new RabbitMQClient(logger, builder.Configuration);
});

builder.Services.AddScoped<EnergySampleStore>();
builder.Services.AddScoped<EnergyQueryService>();
builder.Services.AddHostedService<EnergyMeasurementConsumer>();
builder.Services.AddHostedService<EnergyAggregationService>();

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Sarah Statistics Service API", 
        Version = "v1",
        Description = "API for energy consumption statistics of devices"
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
    var dbContext = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sarah Statistics Service API v1");
    });
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
