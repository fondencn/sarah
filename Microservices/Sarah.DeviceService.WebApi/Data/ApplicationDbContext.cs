using Microsoft.EntityFrameworkCore;
using Sarah.DeviceService.WebApi.Data.Entities;

namespace Sarah.DeviceService.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<DeviceEntity> Devices { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
        });
    }
}
