using Microsoft.EntityFrameworkCore;
using Sarah.Rules.WebApi.Data.Entities;

namespace Sarah.Rules.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<RuleEntity> Rules { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<RuleEntity>(entity =>
        {
            entity.ToTable("Rules");
            entity.HasKey(e => e.Id);
        });
    }
}
