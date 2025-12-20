using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the SecurityClaim entity.
/// </summary>
public class SecurityClaimConfiguration : IEntityTypeConfiguration<SecurityClaim>
{
    public void Configure(EntityTypeBuilder<SecurityClaim> builder)
    {
        // Table name
        builder.ToTable("security_claim");

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
            .HasDatabaseName("uix_security_claim_guid");

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
        builder.Property(e => e.ClaimType)
            .HasColumnName("claim_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ClaimValue)
            .HasColumnName("claim_value")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        // Indexes
        // Unique composite index on (claim_type, claim_value)
        builder.HasIndex(e => new { e.ClaimType, e.ClaimValue })
            .IsUnique()
            .HasDatabaseName("uix_security_claim_type_value");

        // Navigation properties
        builder.HasMany(e => e.RoleClaims)
            .WithOne(rc => rc.SecurityClaim)
            .HasForeignKey(rc => rc.SecurityClaimId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
