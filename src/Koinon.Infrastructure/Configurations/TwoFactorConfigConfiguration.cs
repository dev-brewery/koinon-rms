using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the TwoFactorConfig entity.
/// </summary>
public class TwoFactorConfigConfiguration : IEntityTypeConfiguration<TwoFactorConfig>
{
    public void Configure(EntityTypeBuilder<TwoFactorConfig> builder)
    {
        // Table name
        builder.ToTable("two_factor_config");

        // Primary key
        builder.HasKey(tfc => tfc.Id);
        builder.Property(tfc => tfc.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(tfc => tfc.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(tfc => tfc.Guid)
            .IsUnique()
            .HasDatabaseName("uix_two_factor_config_guid");

        // Ignore computed property
        builder.Ignore(tfc => tfc.IdKey);

        // Foreign key to Person (unique - one config per user)
        builder.Property(tfc => tfc.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.HasIndex(tfc => tfc.PersonId)
            .IsUnique()
            .HasDatabaseName("uix_two_factor_config_person_id");

        // Enabled flag
        builder.Property(tfc => tfc.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        // Secret key (encrypted)
        builder.Property(tfc => tfc.SecretKey)
            .HasColumnName("secret_key")
            .HasMaxLength(64)
            .IsRequired();

        // Recovery codes (JSON array)
        builder.Property(tfc => tfc.RecoveryCodes)
            .HasColumnName("recovery_codes")
            .HasMaxLength(1024);

        // Enabled timestamp
        builder.Property(tfc => tfc.EnabledAt)
            .HasColumnName("enabled_at");

        // Audit fields
        builder.Property(tfc => tfc.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(tfc => tfc.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(tfc => tfc.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(tfc => tfc.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Navigation property
        builder.HasOne(tfc => tfc.Person)
            .WithMany()
            .HasForeignKey(tfc => tfc.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
