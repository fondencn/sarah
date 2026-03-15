using Microsoft.EntityFrameworkCore;
using Sarah.DeviceService.WebApi.Data.Entities;

namespace Sarah.DeviceService.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // New schema-aligned entities from Sarah.Data migration
    public DbSet<DeviceInfoEntity> Devices { get; set; } = null!;
    public DbSet<DeviceTraceEntity> Traces { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
