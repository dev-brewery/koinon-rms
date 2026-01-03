using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for AuditLog entity.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table name (PostgreSQL snake_case convention)
        builder.ToTable("audit_log");

        // Primary key (BIGSERIAL for high-volume logging)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique index
        builder.Property(e => e.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(e => e.Guid)
            .IsUnique()
            .HasDatabaseName("uix_audit_log_guid");

        // IdKey is computed, not stored
        builder.Ignore(e => e.IdKey);

        // Audit fields
        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();
        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        // PersonAlias audit fields (inherited from IAuditable but not used for AuditLog)
        builder.Property(e => e.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");
        builder.Property(e => e.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Foreign key to Person (nullable, SetNull on delete)
        builder.Property(e => e.PersonId)
            .HasColumnName("person_id");

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.SetNull);

        // Action type (indexed for filtering)
        builder.Property(e => e.ActionType)
            .HasColumnName("action_type")
            .HasConversion<int>()
            .IsRequired();

        // Entity type and IdKey
        builder.Property(e => e.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EntityIdKey)
            .HasColumnName("entity_id_key")
            .HasMaxLength(20)
            .IsRequired();

        // Timestamp (indexed for date range queries)
        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_audit_log_timestamp");

        // JSONB columns for PostgreSQL
        builder.Property(e => e.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("jsonb");

        builder.Property(e => e.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("jsonb");

        builder.Property(e => e.ChangedProperties)
            .HasColumnName("changed_properties")
            .HasColumnType("jsonb");

        // IP address (supports IPv6)
        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        // User agent
        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        // Additional info
        builder.Property(e => e.AdditionalInfo)
            .HasColumnName("additional_info");

        // Composite index for entity history
        builder.HasIndex(e => new { e.EntityType, e.EntityIdKey })
            .HasDatabaseName("ix_audit_log_entity_type_entity_id_key");

        // Index on PersonId for user activity queries
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_audit_log_person_id");
    }
}
