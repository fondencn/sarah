using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.SpeechServer;
using Sarah.SpeechServer.Extensions;
using Sarah.Voice;
using Sarah.Messaging.RabbitMQ;
using Sarah.API.Interfaces.Services;
using Sarah.ServiceClients;
using Sarah.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<ILEDService>(provider => LedServiceFactory.CreateLedService(provider));

builder.Services.AddHttpClient<IDeviceService, DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["services__deviceservice__http__0"]
        ?? builder.Configuration["services__deviceservice__http-api__0"]
        ?? builder.Configuration["DeviceServiceUrl"]
        ?? "https+http://deviceservice";
    client.BaseAddress = new Uri(deviceServiceUrl);
})
.AddBearerTokenForwarding();
builder.Services.AddSingleton<ISpeechService, SpeechService>();

// Register MessageBasedWeatherProvider as IWeatherProvider and as IHostedService
builder.Services.AddSingleton<Sarah.SpeechServer.Services.MessageBasedWeatherProvider>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Sarah.SpeechServer.Services.MessageBasedWeatherProvider>());
builder.Services.AddSingleton<IWeatherProvider>(sp => sp.GetRequiredService<Sarah.SpeechServer.Services.MessageBasedWeatherProvider>());

// Register RabbitMQ client and speech event subscriber
builder.Services.AddSingleton<RabbitMQClient>();
builder.Services.AddHostedService<SpeechEventSubscriber>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.MapControllers();
app.UseSpeechService(app.Configuration);

app.Run();
