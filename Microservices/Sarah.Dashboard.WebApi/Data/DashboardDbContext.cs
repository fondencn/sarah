using Microsoft.EntityFrameworkCore;
using Sarah.Dashboard.WebApi.Data.Entities;

namespace Sarah.Dashboard.WebApi.Data;

/// <summary>
/// Database context for the Dashboard service
/// </summary>
public class DashboardDbContext : DbContext
{
    public DashboardDbContext(DbContextOptions<DashboardDbContext> options) 
        : base(options)
    {
    }
    
    /// <summary>
    /// Dashboard items table
    /// </summary>
    public DbSet<DashboardItemEntity> DashboardItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure unique index on ItemId and ItemType combination
        modelBuilder.Entity<DashboardItemEntity>()
            .HasIndex(e => new { e.ItemId, e.ItemType })
            .IsUnique();
    }
}
