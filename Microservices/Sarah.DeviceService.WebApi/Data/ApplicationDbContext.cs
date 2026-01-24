using Microsoft.EntityFrameworkCore;
using Sarah.DeviceService.WebApi.Data.Entities;

namespace Sarah.DeviceService.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    // Legacy DeviceEntity for backward compatibility
    public DbSet<DeviceEntity> LegacyDevices { get; set; } = null!;
    
    // New schema-aligned entities from Sarah.Data migration
    public DbSet<DeviceInfoEntity> Devices { get; set; } = null!;
    public DbSet<DeviceTraceEntity> Traces { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Legacy device entity
        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.ToTable("LegacyDevices");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
        });
        
        // Migrated entities from Sarah.Data
        modelBuilder.Entity<DeviceInfoEntity>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<DeviceTraceEntity>(entity =>
        {
            entity.ToTable("DeviceTraces");
            entity.HasKey(e => e.Id);
        });
    }
}
