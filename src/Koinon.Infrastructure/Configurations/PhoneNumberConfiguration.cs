using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PhoneNumber entity.
/// </summary>
public class PhoneNumberConfiguration : IEntityTypeConfiguration<PhoneNumber>
{
    public void Configure(EntityTypeBuilder<PhoneNumber> builder)
    {
        // Table name
        builder.ToTable("phone_number");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(p => p.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(p => p.Guid)
            .IsUnique()
            .HasDatabaseName("uix_phone_number_guid");

        // Ignore computed properties
        builder.Ignore(p => p.IdKey);

        // Foreign key to Person
        builder.Property(p => p.PersonId)
            .HasColumnName("person_id")
            .IsRequired();
        builder.HasIndex(p => p.PersonId)
            .HasDatabaseName("ix_phone_number_person_id");

        // Configure relationship with cascade delete
        builder.HasOne(p => p.Person)
            .WithMany(person => person.PhoneNumbers)
            .HasForeignKey(p => p.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Phone number (required)
        builder.Property(p => p.Number)
            .HasColumnName("number")
            .HasMaxLength(20)
            .IsRequired();

        // Normalized phone number for fast searching
        builder.Property(p => p.NumberNormalized)
            .HasColumnName("number_normalized")
            .HasMaxLength(20)
            .IsRequired();

        // Index on normalized number for fast phone searches
        builder.HasIndex(p => p.NumberNormalized)
            .HasDatabaseName("ix_phone_number_normalized");

        // Composite index on (PersonId, Number) for lookups and uniqueness
        builder.HasIndex(p => new { p.PersonId, p.Number })
            .HasDatabaseName("ix_phone_number_person_number");

        // Country code
        builder.Property(p => p.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(3);

        // Extension
        builder.Property(p => p.Extension)
            .HasColumnName("extension")
            .HasMaxLength(20);

        // Foreign key to DefinedValue for number type
        builder.Property(p => p.NumberTypeValueId)
            .HasColumnName("number_type_value_id");
        builder.HasIndex(p => p.NumberTypeValueId)
            .HasDatabaseName("ix_phone_number_number_type_value_id");

        // Configure relationship to NumberTypeValue
        builder.HasOne(p => p.NumberTypeValue)
            .WithMany()
            .HasForeignKey(p => p.NumberTypeValueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Messaging enabled flag
        builder.Property(p => p.IsMessagingEnabled)
            .HasColumnName("is_messaging_enabled")
            .IsRequired();

        // Unlisted flag
        builder.Property(p => p.IsUnlisted)
            .HasColumnName("is_unlisted")
            .IsRequired();

        // Description
        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        // Audit fields
        builder.Property(p => p.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(p => p.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(p => p.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(p => p.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");
    }
}
