using Koinon.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Koinon.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating KoinonDbContext instances during EF Core tooling operations
/// (migrations, database updates, etc.).
/// This allows EF Core tools to create the DbContext without running the full application.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KoinonDbContext>
{
    public KoinonDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KoinonDbContext>();

        // Check environment variable first (for CI), then fall back to development defaults
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__KoinonDb")
            ?? "Host=localhost;Port=5432;Database=koinon;Username=koinon;Password=koinon";

        // Use the PostgreSQL provider for configuration
        var provider = new PostgreSqlProvider();
        provider.ConfigureDbContext(optionsBuilder, connectionString);

        // Override some settings for design-time (enable sensitive data logging)
        optionsBuilder.EnableSensitiveDataLogging();

        return new KoinonDbContext(optionsBuilder.Options);
    }
}
