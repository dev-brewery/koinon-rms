using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for SupervisorAuditLog entity.
/// </summary>
public class SupervisorAuditLogConfiguration : IEntityTypeConfiguration<SupervisorAuditLog>
{
    public void Configure(EntityTypeBuilder<SupervisorAuditLog> builder)
    {
        // Table name (PostgreSQL snake_case convention)
        builder.ToTable("supervisor_audit_log");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid (unique, indexed)
        builder.Property(e => e.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(e => e.Guid)
            .HasDatabaseName("ix_supervisor_audit_log_guid")
            .IsUnique();

        // Foreign keys
        builder.Property(e => e.PersonId)
            .HasColumnName("person_id");

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.SupervisorSessionId)
            .HasColumnName("supervisor_session_id");

        builder.HasOne(e => e.SupervisorSession)
            .WithMany()
            .HasForeignKey(e => e.SupervisorSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Action type (indexed)
        builder.Property(e => e.ActionType)
            .HasColumnName("action_type")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(e => e.ActionType)
            .HasDatabaseName("ix_supervisor_audit_log_action_type");

        // IP address
        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45); // IPv6 max length

        // Entity tracking
        builder.Property(e => e.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100);

        builder.Property(e => e.EntityIdKey)
            .HasColumnName("entity_id_key")
            .HasMaxLength(50);

        // Success status
        builder.Property(e => e.Success)
            .HasColumnName("success")
            .IsRequired();

        // Details
        builder.Property(e => e.Details)
            .HasColumnName("details");

        // Audit fields
        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        // Composite index for common queries
        builder.HasIndex(e => new { e.PersonId, e.ActionType })
            .HasDatabaseName("ix_supervisor_audit_log_person_id_action_type");

        // Index for time-based queries
        builder.HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("ix_supervisor_audit_log_created_date_time");

        // Ignore computed properties
        builder.Ignore(e => e.IdKey);
    }
}
