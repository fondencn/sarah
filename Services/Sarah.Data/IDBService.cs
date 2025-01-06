using Microsoft.EntityFrameworkCore;
using Sarah.Data.Models;

namespace Sarah.API.Interfaces.Service
{
    public interface IDBService
    {
        DbSet<DeviceInfo> Devices {get; }
        DbSet<Room> Rooms {get; }
        DbSet<DeviceTraceEntry> Traces {get; }
        DbSet<PersonInfo> Persons {get; }
        DbSet<TemperatureSchedule> TemperatureSchedules {get; }
        DbSet<DeseaseKpi> DeseaseStats {get; }
        DbSet<RuleInfo> RuleInfo {get; }
        DbSet<AlarmSchedule> AlarmSchedule {get; }

    }
}
