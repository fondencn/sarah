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
    }



    /// <summary>
    /// Initializes the required services for the Sarah application.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task InitSarahServices(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<Logger>();
        logger.LogInfo("initializing required services..");

        await app.Services.GetRequiredService<IEventProcessingService>()
            .Start();
        await app.Services.GetRequiredService<IDeviceService>()
            .Start();
        await app.Services.GetRequiredService<IMonitoringService>()
            .Start();

        logger.LogInfo("initialization done.");
    }
}
