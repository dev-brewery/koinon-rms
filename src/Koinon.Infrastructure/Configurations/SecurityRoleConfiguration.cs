using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the SecurityRole entity.
/// </summary>
public class SecurityRoleConfiguration : IEntityTypeConfiguration<SecurityRole>
{
    public void Configure(EntityTypeBuilder<SecurityRole> builder)
    {
        // Table name
        builder.ToTable("security_role");

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
            .HasDatabaseName("uix_security_role_guid");

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
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(e => e.IsSystemRole)
            .HasColumnName("is_system_role")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("uix_security_role_name");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("ix_security_role_is_active");

        // Navigation properties
        builder.HasMany(e => e.PersonRoles)
            .WithOne(pr => pr.SecurityRole)
            .HasForeignKey(pr => pr.SecurityRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.RoleClaims)
            .WithOne(rc => rc.SecurityRole)
            .HasForeignKey(rc => rc.SecurityRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
