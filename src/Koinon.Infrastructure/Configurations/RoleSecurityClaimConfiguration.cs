using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the RoleSecurityClaim entity.
/// </summary>
public class RoleSecurityClaimConfiguration : IEntityTypeConfiguration<RoleSecurityClaim>
{
    public void Configure(EntityTypeBuilder<RoleSecurityClaim> builder)
    {
        // Table name with check constraint
        builder.ToTable("role_security_claim", t =>
            t.HasCheckConstraint("ck_role_claim_allow_deny", "allow_or_deny IN ('A', 'D')"));

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
            .HasDatabaseName("uix_role_security_claim_guid");

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
        builder.Property(e => e.SecurityRoleId)
            .HasColumnName("security_role_id")
            .IsRequired();

        builder.Property(e => e.SecurityClaimId)
            .HasColumnName("security_claim_id")
            .IsRequired();

        builder.Property(e => e.AllowOrDeny)
            .HasColumnName("allow_or_deny")
            .HasColumnType("char(1)")
            .HasComment("A=Allow, D=Deny")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.SecurityRoleId)
            .HasDatabaseName("ix_role_security_claim_security_role_id");

        builder.HasIndex(e => e.SecurityClaimId)
            .HasDatabaseName("ix_role_security_claim_security_claim_id");

        // Unique composite index on (security_role_id, security_claim_id)
        // This ensures a claim can only be assigned once per role
        builder.HasIndex(e => new { e.SecurityRoleId, e.SecurityClaimId })
            .IsUnique()
            .HasDatabaseName("uix_role_security_claim_role_claim");

        // Relationships with restrict delete behavior
        builder.HasOne(e => e.SecurityRole)
            .WithMany(sr => sr.RoleClaims)
            .HasForeignKey(e => e.SecurityRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SecurityClaim)
            .WithMany(sc => sc.RoleClaims)
            .HasForeignKey(e => e.SecurityClaimId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
