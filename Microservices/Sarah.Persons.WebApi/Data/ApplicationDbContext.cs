using Microsoft.EntityFrameworkCore;
using Sarah.Persons.WebApi.Data.Entities;

namespace Sarah.Persons.WebApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<PersonEntity> Persons { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PersonEntity>(entity =>
        {
            entity.ToTable("Persons");
            entity.HasKey(e => e.Id);
        });
    }
}
