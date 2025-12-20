using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PersonSecurityRole entity.
/// </summary>
public class PersonSecurityRoleConfiguration : IEntityTypeConfiguration<PersonSecurityRole>
{
    public void Configure(EntityTypeBuilder<PersonSecurityRole> builder)
    {
        // Table name
        builder.ToTable("person_security_role");

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
            .HasDatabaseName("uix_person_security_role_guid");

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

        builder.Property(e => e.SecurityRoleId)
            .HasColumnName("security_role_id")
            .IsRequired();

        builder.Property(e => e.ExpiresDateTime)
            .HasColumnName("expires_date_time");

        // Indexes
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_person_security_role_person_id");

        builder.HasIndex(e => e.SecurityRoleId)
            .HasDatabaseName("ix_person_security_role_security_role_id");

        builder.HasIndex(e => e.ExpiresDateTime)
            .HasDatabaseName("ix_person_security_role_expires_date_time");

        // Relationships
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SecurityRole)
            .WithMany(sr => sr.PersonRoles)
            .HasForeignKey(e => e.SecurityRoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
