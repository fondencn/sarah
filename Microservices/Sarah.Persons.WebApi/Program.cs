using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Sarah.Persons.WebApi.Data;
using Sarah.Persons.WebApi.Data.Repositories;
using Sarah.Persons.WebApi.Services;
using Sarah.API.Interfaces.Services;
using Sarah.ServiceClients;
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

// Register services
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddSingleton<HomeNetworkService>();

// Register HTTP client for Device Service communication
builder.Services.AddHttpClient<IDeviceService, DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["services__deviceservice__http__0"]
        ?? builder.Configuration["services__deviceservice__http-api__0"]
        ?? builder.Configuration["DeviceServiceUrl"]
        ?? "https+http://deviceservice";
    client.BaseAddress = new Uri(deviceServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddBearerTokenForwarding();

// Register HTTP client for GeoFence Service communication
builder.Services.AddHttpClient<IGeoFenceService, GeoFenceServiceClient>(client =>
{
    var geofenceServiceUrl = builder.Configuration["services__geofencesservice__http__0"]
        ?? builder.Configuration["services__geofencesservice__http-api__0"]
        ?? builder.Configuration["GeoFenceServiceUrl"]
        ?? "https+http://geofencesservice";
    client.BaseAddress = new Uri(geofenceServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddBearerTokenForwarding();

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Sarah Persons Service API", 
        Version = "v1",
        Description = "API for managing persons and their presence in the smart home"
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

try
{
    var homeNetworkService = app.Services.GetRequiredService<HomeNetworkService>();
    await homeNetworkService.Initialize(app.Configuration);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while initializing the home network service.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sarah Persons Service API v1");
    });
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
