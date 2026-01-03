using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the CommunicationPreference entity.
/// </summary>
public class CommunicationPreferenceConfiguration : IEntityTypeConfiguration<CommunicationPreference>
{
    public void Configure(EntityTypeBuilder<CommunicationPreference> builder)
    {
        // Table name
        builder.ToTable("communication_preference");

        // Primary key
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
            .HasDatabaseName("uix_communication_preference_guid");

        // IdKey is computed, not stored
        builder.Ignore(e => e.IdKey);

        // Audit fields
        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();
        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");
        builder.Property(e => e.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");
        builder.Property(e => e.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Regular properties
        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(e => e.CommunicationType)
            .HasColumnName("communication_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.IsOptedOut)
            .HasColumnName("is_opted_out")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.OptOutDateTime)
            .HasColumnName("opt_out_date_time");

        builder.Property(e => e.OptOutReason)
            .HasColumnName("opt_out_reason")
            .HasMaxLength(500);

        // Unique index on person_id + communication_type
        // Each person can only have one preference per communication type
        builder.HasIndex(e => new { e.PersonId, e.CommunicationType })
            .IsUnique()
            .HasDatabaseName("uix_communication_preference_person_type");

        // Index on person_id for lookups
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_communication_preference_person_id");

        // Relationships
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
