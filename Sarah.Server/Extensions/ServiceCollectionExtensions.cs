using Microsoft.EntityFrameworkCore;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data;
using Sarah.Logging;

namespace Sarah.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSarahServices(this IServiceCollection services)
    {
        services.AddSingleton(sp => Logger.Instance);
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Filename=" + ApplicationDbContext.DatabaseFileName));
        services.AddSingleton<INodeFactory, Sarah.NodeFactory.NodeFactory>();
        services.AddSingleton<IDBService>(sp => ApplicationDbContext.CreateDefault());
        services.AddSingleton<IDeviceService, Sarah.DeviceService.DeviceService>();
    }
}
