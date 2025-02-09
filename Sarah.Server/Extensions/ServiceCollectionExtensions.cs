using Microsoft.EntityFrameworkCore;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data;
using Sarah.Logging;
using Sarah.Persons;

namespace Sarah.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSarahServices(this IServiceCollection services)
    {
        services.AddSingleton<Logger>();

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Filename=" + ApplicationDbContext.DatabaseFileName));
        // services.AddDbContextFactory<ApplicationDbContext>(options =>
        //     options.UseSqlite("Filename=" + ApplicationDbContext.DatabaseFileName));
        services.AddSingleton<INodeFactory, Sarah.NodeFactory.NodeFactory>();
        //The services.AddTransient method in ASP.NET Core's dependency injection 
        //system registers a service with a transient lifetime. 
        //This means that a new instance of the service will be created each time 
        //it is requested from the dependency injection container.
        services.AddTransient<IDBService>(sp => ApplicationDbContext.CreateDefault());
        services.AddSingleton<IDeviceService, Sarah.DeviceService.DeviceService>();
        services.AddSingleton<IPersonService, PersonService>();
    }
}
