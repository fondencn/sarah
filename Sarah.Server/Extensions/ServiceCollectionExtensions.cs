using Microsoft.EntityFrameworkCore;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data;
using Sarah.Logging;
using Sarah.Persons;
using Sarah.Geofences;
using Sarah.EventProcessing;
using Sarah.Monitoring;
using Sarah.Rules;
using Sarah.API.Businessobjects;

namespace Sarah.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSarahServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<Logger>();
        string dbFile = configuration["SARAH_DB_PATH"] ?? "./InteLuk.db";
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Filename=" + dbFile));
        // services.AddDbContextFactory<ApplicationDbContext>(options =>
        //     options.UseSqlite("Filename=" + ApplicationDbContext.DatabaseFileName));
        services.AddSingleton<INodeFactory, Sarah.NodeFactory.NodeFactory>();
        services.AddSingleton<IEventProcessingService, EventProcessingService>();
        //The services.AddTransient method in ASP.NET Core's dependency injection 
        //system registers a service with a transient lifetime. 
        //This means that a new instance of the service will be created each time 
        //it is requested from the dependency injection container.
        services.AddTransient<IDBService>(sp => ApplicationDbContext.CreateDefault(configuration));
        services.AddSingleton<IDeviceService, Sarah.DeviceService.DeviceService>();
        services.AddSingleton<IGeoFenceService, GeoFenceService>();
        services.AddSingleton<IPersonService, PersonService>();
        services.AddSingleton<IMonitoringService, MonitoringService>();
        services.AddSingleton<IRuleService, RuleService>();
        services.AddSingleton<IEmailNotifier, DieRooterEmailNotifier>();
    }



    /// <summary>
    /// Initializes the required services for the Sarah application.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task InitSarahServices(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<Logger>();
        var monitorService = app.Services.GetRequiredService<IMonitoringService>();
        var deviceService = app.Services.GetRequiredService<IDeviceService>();
        var eventService = app.Services.GetRequiredService<IEventProcessingService>();
        var personService = app.Services.GetRequiredService<IPersonService>();
        var email = app.Services.GetRequiredService<IEmailNotifier>();
        logger.LogInfo("initializing required services..");

        await eventService
            .Start();
        await deviceService
            .Start();
        await monitorService
            .Start();

        /* Feste Regeln von Christian hinzufügen. 
         * TODO: Move this to external config instead of hardcoding 
         */
        app.Services.GetRequiredService<IRuleService>()
            .RegisterRuleStore(new HardCodedRuleStore(monitorService.Weather, eventService, deviceService, personService, email, monitorService.Ferien));

        logger.LogInfo("initialization done.");
    }
}
