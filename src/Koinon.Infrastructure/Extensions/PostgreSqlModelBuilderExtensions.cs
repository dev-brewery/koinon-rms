using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Koinon.Infrastructure.Extensions;

/// <summary>
/// Extension methods for ModelBuilder to configure PostgreSQL-specific features.
/// Provides helper methods for full-text search, geography types, and other PostgreSQL functionality.
/// </summary>
public static class PostgreSqlModelBuilderExtensions
{
    /// <summary>
    /// Configures full-text search for the Person entity.
    /// Creates a generated tsvector column and GIN index for fast text searching.
    /// </summary>
    public static ModelBuilder ConfigurePersonFullTextSearch(this ModelBuilder modelBuilder)
    {
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

        return modelBuilder;
    }

    /// <summary>
    /// Configures PostGIS geography columns for the Location entity.
    /// Enables spatial queries and indexing.
    /// </summary>
    public static ModelBuilder ConfigureLocationGeography(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>(entity =>
        {
            // GeoPoint is configured as GEOGRAPHY(POINT, 4326) via NetTopologySuite
            // The configuration is already in LocationConfiguration.cs
            // This method provides a centralized place for any additional geography setup

            // Future: Add spatial indexes if needed
            // entity.HasIndex(l => l.GeoPoint)
            //     .HasDatabaseName("ix_location_geo_point")
            //     .HasMethod("GIST");
        });

        return modelBuilder;
    }

    /// <summary>
    /// Ensures PostGIS extension is enabled in the database.
    /// This should be called in migrations to ensure the extension exists.
    /// </summary>
    public static ModelBuilder EnsurePostGisExtension(this ModelBuilder modelBuilder)
    {
        // This is typically done in migrations with:
        // migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");

        // However, we can use HasPostgresExtension for EF Core to track it
        modelBuilder.HasPostgresExtension("postgis");

        return modelBuilder;
    }

    /// <summary>
    /// Configures array types commonly used in PostgreSQL.
    /// </summary>
    public static ModelBuilder ConfigureArrayTypes(this ModelBuilder modelBuilder)
    {
        // Future: Configure entities that use PostgreSQL arrays
        // For example, if we add tags or attributes as arrays
        // entity.Property(e => e.Tags)
        //     .HasColumnType("text[]");

        return modelBuilder;
    }

    /// <summary>
    /// Configures JSONB columns for semi-structured data.
    /// </summary>
    public static ModelBuilder ConfigureJsonColumns(this ModelBuilder modelBuilder)
    {
        // Future: Configure entities that use JSONB columns
        // For example, storing flexible attributes or settings
        // entity.Property(e => e.Attributes)
        //     .HasColumnType("jsonb");

        return modelBuilder;
    }

    /// <summary>
    /// Applies all PostgreSQL-specific configurations.
    /// Convenience method to apply all PostgreSQL features at once.
    /// </summary>
    public static ModelBuilder ApplyPostgreSqlConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder
            .EnsurePostGisExtension()
            .ConfigurePersonFullTextSearch()
            .ConfigureLocationGeography()
            .ConfigureArrayTypes()
            .ConfigureJsonColumns();

        return modelBuilder;
    }
}
