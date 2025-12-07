using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for SupervisorSession entity.
/// </summary>
public class SupervisorSessionConfiguration : IEntityTypeConfiguration<SupervisorSession>
{
    public void Configure(EntityTypeBuilder<SupervisorSession> builder)
    {
        // Table name (PostgreSQL snake_case convention)
        builder.ToTable("supervisor_session");

        // Primary key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid (unique, indexed)
        builder.Property(s => s.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(s => s.Guid)
            .HasDatabaseName("uix_supervisor_session_guid")
            .IsUnique();

        // Foreign keys
        builder.Property(s => s.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.HasOne(s => s.Person)
            .WithMany()
            .HasForeignKey(s => s.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Token (unique, indexed)
        builder.Property(s => s.Token)
            .HasColumnName("token")
            .HasMaxLength(256)
            .IsRequired();
        builder.HasIndex(s => s.Token)
            .HasDatabaseName("uix_supervisor_session_token")
            .IsUnique();

        // Expiration
        builder.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
        builder.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("ix_supervisor_session_expires_at");

        // Ended timestamp
        builder.Property(s => s.EndedAt)
            .HasColumnName("ended_at");

        // IP tracking
        builder.Property(s => s.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45); // IPv6 max length

        // Audit fields
        builder.Property(s => s.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(s => s.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(s => s.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(s => s.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Ignore computed properties
        builder.Ignore(s => s.IdKey);
        builder.Ignore(s => s.IsActive);
    }
}
