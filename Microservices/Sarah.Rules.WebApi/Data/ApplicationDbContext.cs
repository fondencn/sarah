using Microsoft.EntityFrameworkCore;
using Sarah.Rules.WebApi.Data.Entities;
using Sarah.Rules.Data.Entities;

namespace Sarah.Rules.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<AlarmScheduleEntity> AlarmSchedules { get; set; } = null!;
    public DbSet<TemperatureScheduleEntity> TemperatureSchedules { get; set; } = null!;
    public DbSet<RuleExecutionLogEntity> RuleExecutionLogs { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
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

        modelBuilder.Entity<RuleExecutionLogEntity>(entity =>
        {
            entity.ToTable("RuleExecutionLogs");
            entity.HasKey(e => e.Id);
        });
    }
}
