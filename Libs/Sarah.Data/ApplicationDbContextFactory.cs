using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Sarah.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Configure the DbContext to use SQLite
            optionsBuilder.UseSqlite(ApplicationDbContext.DBPath);

            return new ApplicationDbContext(optionsBuilder.Options, null);
        }
    }
}