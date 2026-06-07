using Microsoft.EntityFrameworkCore;
using Sarah.Persons.WebApi.Data.Entities;

namespace Sarah.Persons.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    // Legacy entity
    public DbSet<PersonEntity> LegacyPersons { get; set; } = null!;
    
    // Migrated entities from Sarah.Data
    public DbSet<PersonInfoEntity> Persons { get; set; } = null!;
    public DbSet<UserFavouriteEntity> UserFavourites { get; set; } = null!;
    public DbSet<NamedPositionCacheEntity> NamedPositionCache { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<PersonEntity>(entity =>
        {
            entity.ToTable("LegacyPersons");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<PersonInfoEntity>(entity =>
        {
            entity.ToTable("Persons");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<UserFavouriteEntity>(entity =>
        {
            entity.ToTable("UserFavourites");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<NamedPositionCacheEntity>(entity =>
        {
            entity.ToTable("NamedPositionCache");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Latitude, e.Longitude }).IsUnique();
        });
    }
}
