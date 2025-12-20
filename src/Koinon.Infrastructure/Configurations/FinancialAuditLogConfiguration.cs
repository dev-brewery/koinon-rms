using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for FinancialAuditLog entity.
/// </summary>
public class FinancialAuditLogConfiguration : IEntityTypeConfiguration<FinancialAuditLog>
{
    public void Configure(EntityTypeBuilder<FinancialAuditLog> builder)
    {
        // Table name (PostgreSQL snake_case convention)
        builder.ToTable("financial_audit_log");

        // Primary key (BIGSERIAL for high-volume logging)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Foreign key to Person
        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Action type (indexed for filtering)
        builder.Property(e => e.ActionType)
            .HasColumnName("action_type")
            .IsRequired();

        builder.HasIndex(e => e.ActionType)
            .HasDatabaseName("ix_financial_audit_action");

        // Entity type and IdKey
        builder.Property(e => e.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EntityIdKey)
            .HasColumnName("entity_id_key")
            .HasMaxLength(20)
            .IsRequired();

        // Composite index for entity lookups
        builder.HasIndex(e => new { e.EntityType, e.EntityIdKey })
            .HasDatabaseName("ix_financial_audit_entity");

        // IP address (supports IPv6)
        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        // User agent
        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        // Details (JSONB for PostgreSQL)
        builder.Property(e => e.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        // Timestamp (indexed for time-based queries)
        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_financial_audit_timestamp");

        // Index on PersonId for user activity queries
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_financial_audit_person");
    }
}
