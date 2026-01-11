using Microsoft.EntityFrameworkCore;
using Sarah.Geofences.WebApi.Data.Entities;

namespace Sarah.Geofences.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<GeofenceEntity> Geofences { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<GeofenceEntity>(entity =>
        {
            entity.ToTable("Geofences");
            entity.HasKey(e => e.Id);
        });
    }
}
