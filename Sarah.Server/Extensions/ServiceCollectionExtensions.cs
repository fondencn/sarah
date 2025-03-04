using Microsoft.EntityFrameworkCore;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data;
using Sarah.Logging;
using Sarah.Persons;
using Sarah.Geofences;
using Sarah.EventProcessing;

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
    }
}
