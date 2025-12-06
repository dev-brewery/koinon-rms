using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PersonAlias entity.
/// </summary>
public class PersonAliasConfiguration : IEntityTypeConfiguration<PersonAlias>
{
    public void Configure(EntityTypeBuilder<PersonAlias> builder)
    {
        // Table name
        builder.ToTable("person_alias");

        // Primary key
        builder.HasKey(pa => pa.Id);
        builder.Property(pa => pa.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(pa => pa.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(pa => pa.Guid)
            .IsUnique()
            .HasDatabaseName("uix_person_alias_guid");

        // Ignore computed IdKey
        builder.Ignore(pa => pa.IdKey);

        // PersonId (nullable FK - can be null for merge-only records)
        builder.Property(pa => pa.PersonId)
            .HasColumnName("person_id");

        builder.HasIndex(pa => pa.PersonId)
            .HasDatabaseName("ix_person_alias_person_id")
            .HasFilter("person_id IS NOT NULL");

        // Name
        builder.Property(pa => pa.Name)
            .HasColumnName("name")
            .HasMaxLength(250);

        // AliasPersonId (for merged person tracking)
        builder.Property(pa => pa.AliasPersonId)
            .HasColumnName("alias_person_id");

        // AliasPersonGuid (for merged person tracking)
        builder.Property(pa => pa.AliasPersonGuid)
            .HasColumnName("alias_person_guid");

        // Audit fields
        builder.Property(pa => pa.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(pa => pa.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(pa => pa.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(pa => pa.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Relationship to Person (optional - can be null for merge-only records)
        builder.HasOne(pa => pa.Person)
            .WithMany()
            .HasForeignKey(pa => pa.PersonId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
