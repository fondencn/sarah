using Microsoft.EntityFrameworkCore;
using Sarah.Monitoring.WebApi.Data.Entities;

namespace Sarah.Monitoring.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<MonitoringDataEntity> MonitoringDatas { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MonitoringDataEntity>(entity =>
        {
            entity.ToTable("MonitoringDatas");
            entity.HasKey(e => e.Id);
        });
    }
}
