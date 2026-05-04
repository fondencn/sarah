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
    public DbSet<KernelConversationMessageEntity> KernelConversationMessages { get; set; } = null!;
    public DbSet<PromptRuleEntity> PromptRules { get; set; } = null!;
    
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

        modelBuilder.Entity<KernelConversationMessageEntity>(entity =>
        {
            entity.ToTable("KernelConversationMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConversationId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAtUtc });
        });

        modelBuilder.Entity<PromptRuleEntity>(entity =>
        {
            entity.ToTable("PromptRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Guidance).IsRequired();
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.SortOrder).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.IsEnabled);
        });
    }
}
