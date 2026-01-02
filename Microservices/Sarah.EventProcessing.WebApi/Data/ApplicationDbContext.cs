using Microsoft.EntityFrameworkCore;
using Sarah.EventProcessing.WebApi.Data.Entities;

namespace Sarah.EventProcessing.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<EventEntity> Events { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.Id);
        });
    }
}
