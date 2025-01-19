using Microsoft.EntityFrameworkCore;
using Sarah.Data.Models;

namespace Sarah.API.Interfaces.Service
{
    /// <summary>
    /// Interface for database service providing access to various DbSet properties
    /// and a method to save changes to the database.
    /// </summary>
    public interface IDBService
    {
        /// <summary>
        /// Gets the DbSet of DeviceInfo entities.
        /// </summary>
        DbSet<DeviceInfo> Devices { get; }

        /// <summary>
        /// Gets the DbSet of Room entities.
        /// </summary>
        DbSet<Room> Rooms { get; }

        /// <summary>
        /// Gets the DbSet of DeviceTraceEntry entities.
        /// </summary>
        DbSet<DeviceTraceEntry> Traces { get; }

        /// <summary>
        /// Gets the DbSet of PersonInfo entities.
        /// </summary>
        DbSet<PersonInfo> Persons { get; }

        /// <summary>
        /// Gets the DbSet of TemperatureSchedule entities.
        /// </summary>
        DbSet<TemperatureSchedule> TemperatureSchedules { get; }

        /// <summary>
        /// Gets the DbSet of DeseaseKpi entities.
        /// </summary>
        DbSet<DeseaseKpi> DeseaseStats { get; }

        /// <summary>
        /// Gets the DbSet of RuleInfo entities.
        /// </summary>
        DbSet<RuleInfo> RuleInfo { get; }

        /// <summary>
        /// Gets the DbSet of AlarmSchedule entities.
        /// </summary>
        DbSet<AlarmSchedule> AlarmSchedule { get; }

        /// <summary>
        /// Gets the DbSet of UserFavourites entities.
        /// </summary>
        DbSet<UserFavourite> UserFavourites { get; }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> SaveChangesAsync();
    }
}

