using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql;
using NpgsqlTypes;

namespace Koinon.Infrastructure.Providers;

/// <summary>
/// PostgreSQL-specific database provider implementation.
/// Configures PostgreSQL features including full-text search, geography types, and naming conventions.
/// </summary>
public class PostgreSqlProvider : IDatabaseProvider
{
    /// <summary>
    /// Configures the DbContext to use PostgreSQL with Npgsql provider.
    /// </summary>
    public void ConfigureDbContext(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                // Enable PostGIS for geography/geometry types (for Location.GeoPoint, GeoFence)
                npgsqlOptions.UseNetTopologySuite();

                // Set migrations assembly
                npgsqlOptions.MigrationsAssembly(typeof(PostgreSqlProvider).Assembly.FullName);

                // Command timeout for long-running migrations
                npgsqlOptions.CommandTimeout(120);

                // Enable retry on transient failures
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

        // Performance optimizations
        optionsBuilder
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // Default to no tracking for better performance
    }

    /// <summary>
    /// Configures full-text search vectors and indexes for searchable entities.
    /// </summary>
    public void ConfigureFullTextSearch(ModelBuilder modelBuilder)
    {
        // Configure Person full-text search
        // Creates a generated tsvector column that combines FirstName, LastName, NickName, and Email
        // with different weights (A = highest relevance, C = lowest relevance)
        modelBuilder.Entity<Person>(entity =>
        {
            // Add the search_vector column as a computed/generated column
            entity.Property<NpgsqlTsVector>("SearchVector")
                .HasColumnName("search_vector")
                .HasComputedColumnSql(
                    @"
                    setweight(to_tsvector('english', coalesce(first_name, '')), 'A') ||
                    setweight(to_tsvector('english', coalesce(last_name, '')), 'A') ||
                    setweight(to_tsvector('english', coalesce(nick_name, '')), 'B') ||
                    setweight(to_tsvector('english', coalesce(email, '')), 'C')
                    ",
                    stored: true);

            // Create GIN index on the search_vector for fast full-text queries
            entity.HasIndex("SearchVector")
                .HasDatabaseName("ix_person_search_vector")
                .HasMethod("GIN");
        });

        // Additional full-text search configurations can be added here for other entities
        // For example, Group names, Location names, etc.
    }

    /// <summary>
    /// Gets the PostgreSQL-specific full-text search query for Person.
    /// Uses the ts_query syntax with the search_vector column.
    /// </summary>
    public string GetPersonSearchQuery(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return string.Empty;
        }

        // Clean and prepare the search term
        // Replace spaces with & (AND operator in tsquery)
        // Add :* to enable prefix matching
        var sanitized = searchTerm.Trim()
            .Replace("'", "''") // Escape single quotes
            .Replace(" ", " & "); // AND operator between terms

        // Return the WHERE clause fragment for use in LINQ queries
        // This will be used with EF.Functions.ToTsQuery or raw SQL
        return $"search_vector @@ to_tsquery('english', '{sanitized}:*')";
    }

    /// <summary>
    /// Configures PostgreSQL-specific conventions.
    /// </summary>
    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // PostgreSQL uses snake_case by convention, but we're explicitly setting
        // column and table names in each EntityTypeConfiguration to maintain control.
        // This method is available for future global conventions if needed.

        // Example future conventions:
        // - All decimal types use specific precision
        // - All datetime types use timestamptz
        // - All string types use specific collation
    }

    /// <summary>
    /// Creates a connection string builder with common PostgreSQL options.
    /// </summary>
    public static NpgsqlConnectionStringBuilder CreateConnectionStringBuilder(
        string host,
        int port,
        string database,
        string username,
        string password)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 20,
            ConnectionLifetime = 300, // 5 minutes
            Timeout = 30,
            CommandTimeout = 30,
            // Include error detail for better debugging
            IncludeErrorDetail = true,
            // Application name for PostgreSQL connection tracking
            ApplicationName = "Koinon.RMS"
        };
    }
}
