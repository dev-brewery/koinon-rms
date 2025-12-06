using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for RefreshToken entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table name
        builder.ToTable("refresh_token");

        // Primary key
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid
        builder.Property(rt => rt.Guid)
            .HasColumnName("guid")
            .IsRequired();

        builder.HasIndex(rt => rt.Guid)
            .HasDatabaseName("uix_refresh_token_guid")
            .IsUnique();

        // Foreign keys
        builder.Property(rt => rt.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        // Token
        builder.Property(rt => rt.Token)
            .HasColumnName("token")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(rt => rt.Token)
            .HasDatabaseName("uix_refresh_token_token")
            .IsUnique();

        // Expiration
        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("ix_refresh_token_expires_at");

        // Revocation
        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(rt => rt.ReplacedByToken)
            .HasColumnName("replaced_by_token")
            .HasMaxLength(256);

        // IP addresses
        builder.Property(rt => rt.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45); // IPv6 max length

        builder.Property(rt => rt.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasMaxLength(45);

        // Audit fields (inherited from Entity)
        builder.Property(rt => rt.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(rt => rt.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(rt => rt.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(rt => rt.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Relationships
        builder.HasOne(rt => rt.Person)
            .WithMany()
            .HasForeignKey(rt => rt.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(rt => rt.IdKey);
        builder.Ignore(rt => rt.IsActive);
    }
}
