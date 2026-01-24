using Sarah.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Repositories;
using Sarah.Rules.Clients;
using Sarah.Messaging.RabbitMQ;
using Sarah.Rules.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Bearer Token Authentication with Keycloak
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register HTTP client for DeviceService communication
builder.Services.AddHttpClient<IDeviceServiceClient, DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["DeviceServiceUrl"] ?? "http://deviceservice:5001";
    client.BaseAddress = new Uri(deviceServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register RabbitMQ client
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMQClient>>();
    return new RabbitMQClient(logger, builder.Configuration);
});

// Register RuleService as singleton (will be used by RulesEventSubscriber)
// Note: RuleService still has dependencies on legacy interfaces, this is for backwards compatibility
builder.Services.AddSingleton<Sarah.Rules.HardCodedRuleStore>();
builder.Services.AddSingleton<Sarah.Rules.RuleService>();

// Register RulesEventSubscriber as a hosted service
builder.Services.AddHostedService<RulesEventSubscriber>();

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// register the hardcoded rule store    
var ruleSvc = app.Services.GetRequiredService<Sarah.Rules.RuleService>();
var hardCoded = app.Services.GetRequiredService<Sarah.Rules.HardCodedRuleStore>();

ruleSvc.RegisterRuleStore(hardCoded);


app.Run();
