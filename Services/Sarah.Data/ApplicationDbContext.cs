using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Sarah.API.Interfaces.Service;
using Sarah.Data.Models;
using Sarah.Logging;

namespace Sarah.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, string>, IDBService
    {
        public static string DatabaseFileName => "./InteLuk.db";

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected ApplicationDbContext()
        {
        }

        public static IDBService CreateDefault()
        {
            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
            Logger.Instance.LogDebug("Using database at " + Path.GetFullPath( DatabaseFileName));
            builder.UseSqlite("Filename=" + DatabaseFileName);
            return new ApplicationDbContext(builder.Options);
        }

        public Task<int> SaveChangesAsync()
        {
            return this.SaveChangesAsync(CancellationToken.None);
        }

        public DbSet<DeviceInfo> Devices => this.Set<DeviceInfo>();
        public DbSet<Room> Rooms => this.Set<Room>();
        public DbSet<DeviceTraceEntry> Traces => this.Set<DeviceTraceEntry>();
        public DbSet<PersonInfo> Persons => this.Set<PersonInfo>();
        public DbSet<TemperatureSchedule> TemperatureSchedules => this.Set<TemperatureSchedule>();
        public DbSet<DeseaseKpi> DeseaseStats => this.Set<DeseaseKpi>();
        public DbSet<RuleInfo> RuleInfo => this.Set<RuleInfo>();
        public DbSet<AlarmSchedule> AlarmSchedule => this.Set<AlarmSchedule>();
        public DbSet<UserFavourite> UserFavourites => this.Set<UserFavourite>();


    }

}
