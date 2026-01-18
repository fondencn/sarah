using Microsoft.EntityFrameworkCore;
using Sarah.Monitoring.WebApi.Data.Entities;

namespace Sarah.Monitoring.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    // Legacy entity
    public DbSet<MonitoringDataEntity> MonitoringDatas { get; set; } = null!;
    
    // Migrated entities from Sarah.Data
    public DbSet<DeviceInfoEntity> Devices { get; set; } = null!;
    public DbSet<RoomEntity> Rooms { get; set; } = null!;
    public DbSet<PersonInfoEntity> Persons { get; set; } = null!;
    public DbSet<AlarmScheduleEntity> AlarmSchedule { get; set; } = null!;
    public DbSet<TemperatureScheduleEntity> TemperatureSchedules { get; set; } = null!;
    public DbSet<DeseaseKpiEntity> DeseaseStats { get; set; } = null!;
    public DbSet<DeviceTraceEntity> Traces { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<MonitoringDataEntity>(entity =>
        {
            entity.ToTable("MonitoringDatas");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<DeviceInfoEntity>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<RoomEntity>(entity =>
        {
            entity.ToTable("Rooms");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<PersonInfoEntity>(entity =>
        {
            entity.ToTable("Persons");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<AlarmScheduleEntity>(entity =>
        {
            entity.ToTable("AlarmSchedules");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<TemperatureScheduleEntity>(entity =>
        {
            entity.ToTable("TemperatureSchedules");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<DeseaseKpiEntity>(entity =>
        {
            entity.ToTable("DeseaseKpis");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<DeviceTraceEntity>(entity =>
        {
            entity.ToTable("DeviceTraces");
            entity.HasKey(e => e.Id);
        });
    }
}
