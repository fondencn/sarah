using Microsoft.EntityFrameworkCore;

namespace Sarah.Statistics.WebApi.Data;

public class StatisticsDbContext : DbContext
{
    public StatisticsDbContext(DbContextOptions<StatisticsDbContext> options) : base(options) { }

    public DbSet<EnergySample> EnergySamples { get; set; } = null!;
    public DbSet<EnergyAggregate> EnergyAggregates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EnergySample>(entity =>
        {
            entity.ToTable("EnergySamples");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp });
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.DeviceName).HasMaxLength(200);
        });

        modelBuilder.Entity<EnergyAggregate>(entity =>
        {
            entity.ToTable("EnergyAggregates");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DeviceId, e.Granularity, e.BucketStart }).IsUnique();
            entity.HasIndex(e => new { e.Granularity, e.BucketStart });
            entity.Property(e => e.DeviceName).HasMaxLength(200);
        });
    }
}
