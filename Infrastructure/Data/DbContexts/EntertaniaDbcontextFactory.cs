using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Infrastructure.Data.DbContexts
{
    /// <summary>
    /// Design-time factory for creating EntertaniaDbcontext during migrations.
    /// This allows EF Core tools to instantiate the DbContext without running the application.
    /// </summary>
    public class EntertaniaDbcontextFactory : IDesignTimeDbContextFactory<EntertaniaDbcontext>
    {
        public EntertaniaDbcontext CreateDbContext(string[] args)
        {
            // Build configuration to read from appsettings.json in the Web project
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // Try to load from current directory first (Infrastructure project)
                .AddJsonFile("appsettings.json", optional: true)
                // Then try parent directory structure to find the Web project's appsettings
                .AddJsonFile(Path.Combine("..", "Entertania1", "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine("..", "..", "Entertania1", "appsettings.json"), optional: true)
                .Build();

            // Get connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // If still not found, use hardcoded fallback (same as in your DbContext)
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=.;Initial Catalog=EntertaniaDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True";
            }

            // Create DbContextOptions
            var optionsBuilder = new DbContextOptionsBuilder<EntertaniaDbcontext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new EntertaniaDbcontext(optionsBuilder.Options);
        }
    }
}