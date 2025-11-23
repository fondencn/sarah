using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.EventProcessing;
using Sarah.LEDService;
using Sarah.SpeechServer;
using Sarah.SpeechServer.Extensions;
using Sarah.Voice;
using Sarah.Voice.DeviceApi;
using Services.Sarah.API.Interfaces.Service;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<ILEDService, ReSpeakerLEDService>();
builder.Services.AddSingleton<IEventProcessingService, EventProcessingService>();
builder.Services.AddSingleton<IDeviceServiceClient, DeviceServiceClient>();
builder.Services.AddSingleton<ISpeechService, SpeechService>();
builder.Services.AddSingleton<SpeechEventSubscriber>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.MapControllers();
app.UseHttpsRedirection();
app.UseSpeechService(app.Configuration);
await app.UseSpeechEvents();

app.Run();

