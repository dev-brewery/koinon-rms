using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the AuthorizedPickup entity.
/// </summary>
public class AuthorizedPickupConfiguration : IEntityTypeConfiguration<AuthorizedPickup>
{
    public void Configure(EntityTypeBuilder<AuthorizedPickup> builder)
    {
        // Table name
        builder.ToTable("authorized_pickup");

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
            .HasDatabaseName("uix_authorized_pickup_guid");

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

        // Foreign keys
        builder.Property(e => e.ChildPersonId)
            .HasColumnName("child_person_id")
            .IsRequired();

        builder.Property(e => e.AuthorizedPersonId)
            .HasColumnName("authorized_person_id");

        // Regular properties
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200);

        builder.Property(e => e.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(50);

        builder.Property(e => e.Relationship)
            .HasColumnName("relationship")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(PickupRelationship.Other);

        builder.Property(e => e.AuthorizationLevel)
            .HasColumnName("authorization_level")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(AuthorizationLevel.Always);

        builder.Property(e => e.PhotoUrl)
            .HasColumnName("photo_url")
            .HasMaxLength(500);

        builder.Property(e => e.CustodyNotes)
            .HasColumnName("custody_notes");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(e => e.ChildPerson)
            .WithMany()
            .HasForeignKey(e => e.ChildPersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AuthorizedPerson)
            .WithMany()
            .HasForeignKey(e => e.AuthorizedPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        // Main lookup by child
        builder.HasIndex(e => e.ChildPersonId)
            .HasDatabaseName("ix_authorized_pickup_child_person_id");

        // Composite index for filtering by child and authorization level
        builder.HasIndex(e => new { e.ChildPersonId, e.AuthorizationLevel })
            .HasDatabaseName("ix_authorized_pickup_child_person_id_authorization_level");

        // Reverse lookup by authorized person
        builder.HasIndex(e => e.AuthorizedPersonId)
            .HasDatabaseName("ix_authorized_pickup_authorized_person_id")
            .HasFilter("authorized_person_id IS NOT NULL");
    }
}
