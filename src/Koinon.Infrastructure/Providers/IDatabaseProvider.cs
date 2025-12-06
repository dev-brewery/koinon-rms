using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Providers;

/// <summary>
/// Interface for database provider-specific configurations and features.
/// Abstracts database-specific functionality (PostgreSQL vs SQL Server) from the main DbContext.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Configures the DbContextOptionsBuilder with provider-specific options.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder to configure.</param>
    /// <param name="connectionString">The database connection string.</param>
    void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string connectionString);

    /// <summary>
    /// Configures full-text search capabilities for entities that support it.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    void ConfigureFullTextSearch(ModelBuilder modelBuilder);

    /// <summary>
    /// Gets the SQL query fragment for performing full-text search on the Person entity.
    /// </summary>
    /// <param name="searchTerm">The search term to query for.</param>
    /// <returns>SQL fragment for WHERE clause or empty string if search term is invalid.</returns>
    string GetPersonSearchQuery(string searchTerm);

    /// <summary>
    /// Configures provider-specific conventions (e.g., naming conventions).
    /// </summary>
    /// <param name="configurationBuilder">The model configuration builder.</param>
    void ConfigureConventions(ModelConfigurationBuilder configurationBuilder);
}
