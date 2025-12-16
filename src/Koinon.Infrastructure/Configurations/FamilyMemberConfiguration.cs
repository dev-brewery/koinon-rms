using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the FamilyMember entity.
/// </summary>
public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        // Table name
        builder.ToTable("family_member");

        // Primary key
        builder.HasKey(fm => fm.Id);
        builder.Property(fm => fm.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(fm => fm.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(fm => fm.Guid)
            .IsUnique()
            .HasDatabaseName("uix_family_member_guid");

        // Ignore computed properties
        builder.Ignore(fm => fm.IdKey);

        // Foreign keys
        builder.Property(fm => fm.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(fm => fm.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(fm => fm.FamilyRoleId)
            .HasColumnName("family_role_id")
            .IsRequired();

        // Properties
        builder.Property(fm => fm.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(fm => fm.DateAdded)
            .HasColumnName("date_added")
            .IsRequired();

        // Audit fields
        builder.Property(fm => fm.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(fm => fm.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        // Relationships
        builder.HasOne(fm => fm.Family)
            .WithMany(f => f.Members)
            .HasForeignKey(fm => fm.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fm => fm.Person)
            .WithMany(p => p.FamilyMemberships)
            .HasForeignKey(fm => fm.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fm => fm.FamilyRole)
            .WithMany()
            .HasForeignKey(fm => fm.FamilyRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(fm => fm.FamilyId)
            .HasDatabaseName("ix_family_member_family_id");

        builder.HasIndex(fm => fm.PersonId)
            .HasDatabaseName("ix_family_member_person_id");

        builder.HasIndex(fm => fm.FamilyRoleId)
            .HasDatabaseName("ix_family_member_family_role_id");

        // Unique constraint: a person can only be in a family once
        builder.HasIndex(fm => new { fm.FamilyId, fm.PersonId })
            .IsUnique()
            .HasDatabaseName("uix_family_member_family_person");

        // Index for finding primary family
        builder.HasIndex(fm => new { fm.PersonId, fm.IsPrimary })
            .HasDatabaseName("ix_family_member_person_primary");
    }
}
