using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Sarah.Dashboard.WebApi.Data;
using Sarah.Dashboard.WebApi.Data.Repositories;
using Sarah.Dashboard.WebApi.Services;
using Sarah.Dashboard.WebApi.Data.Entities;
using Sarah.ServiceClients;
using Sarah.ServiceDefaults;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure JWT Bearer Token Authentication with Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<DashboardDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"))
        .ConfigureWarnings(warnings => warnings.Log(
            (RelationalEventId.CommandExecuting, LogLevel.Debug),
            (RelationalEventId.CommandExecuted, LogLevel.Debug))));

// Register repositories
builder.Services.AddScoped<IRepository<DashboardItemEntity>, Repository<DashboardItemEntity>>(sp =>
    new Repository<DashboardItemEntity>(sp.GetRequiredService<DashboardDbContext>()));

// Register services
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Register HTTP client for DeviceService communication
builder.Services.AddHttpClient<DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["services__deviceservice__http__0"]
        ?? builder.Configuration["services__deviceservice__http-api__0"]
        ?? builder.Configuration["DeviceServiceUrl"]
        ?? "http://deviceservice:8080";
    client.BaseAddress = new Uri(deviceServiceUrl);
})
.AddBearerTokenForwarding();

// Register HTTP client for PersonService communication
builder.Services.AddHttpClient<PersonServiceClient>(client =>
{
    var personServiceUrl = builder.Configuration["services__personsservice__http__0"]
        ?? builder.Configuration["services__personsservice__http-api__0"]
        ?? builder.Configuration["PersonServiceUrl"]
        ?? "http://personsservice:8080";
    client.BaseAddress = new Uri(personServiceUrl);
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
        Title = "Sarah Dashboard Service API", 
        Version = "v1",
        Description = "API for managing dashboard items in the smart home"
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
    var dbContext = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sarah Dashboard Service API v1");
    });
}

app.UseServiceDefaults();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
