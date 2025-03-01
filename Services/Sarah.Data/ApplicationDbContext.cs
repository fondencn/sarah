using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sarah.API.Interfaces.Service;
using Sarah.Data.Models;
using Sarah.Logging;

namespace Sarah.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, string>, IDBService
    {
        private readonly IConfiguration? _configuration;

        public static string DBPath {get; private set;} = Environment.GetEnvironmentVariable("SARAH_DB_PATH") ?? "./InteLuk.db";

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration? configuration) : base(options)
        {
            _configuration = configuration;
        }

        protected ApplicationDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var databaseFileName = _configuration?["SARAH_DB_PATH"] ?? "./InteLuk.db";
                DBPath = databaseFileName;
                optionsBuilder.UseSqlite($"Filename={databaseFileName}");
            }
        }

        public static IDBService CreateDefault(IConfiguration configuration)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var databaseFileName = configuration?["SARAH_DB_PATH"] ?? "./InteLuk.db";

            Logger.Instance.LogDebug("Using database at " + Path.GetFullPath(databaseFileName));
            builder.UseSqlite($"Filename={databaseFileName}");
            DBPath = databaseFileName;

            return new ApplicationDbContext(builder.Options, configuration);
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
