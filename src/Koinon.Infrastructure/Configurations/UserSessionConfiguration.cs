using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the UserSession entity.
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        // Table name
        builder.ToTable("user_session");

        // Primary key
        builder.HasKey(us => us.Id);
        builder.Property(us => us.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(us => us.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(us => us.Guid)
            .IsUnique()
            .HasDatabaseName("uix_user_session_guid");

        // Ignore computed property
        builder.Ignore(us => us.IdKey);

        // Foreign key to Person
        builder.Property(us => us.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.HasIndex(us => us.PersonId)
            .HasDatabaseName("ix_user_session_person_id");

        // Foreign key to RefreshToken (optional)
        builder.Property(us => us.RefreshTokenId)
            .HasColumnName("refresh_token_id");

        builder.HasIndex(us => us.RefreshTokenId)
            .HasDatabaseName("ix_user_session_refresh_token_id");

        // Device info
        builder.Property(us => us.DeviceInfo)
            .HasColumnName("device_info")
            .HasMaxLength(256);

        // IP address
        builder.Property(us => us.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsRequired();

        // Location
        builder.Property(us => us.Location)
            .HasColumnName("location")
            .HasMaxLength(128);

        // Last activity timestamp
        builder.Property(us => us.LastActivityAt)
            .HasColumnName("last_activity_at")
            .IsRequired();

        // Active flag
        builder.Property(us => us.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Audit fields
        builder.Property(us => us.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(us => us.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(us => us.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(us => us.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Navigation properties
        builder.HasOne(us => us.Person)
            .WithMany()
            .HasForeignKey(us => us.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(us => us.RefreshToken)
            .WithMany()
            .HasForeignKey(us => us.RefreshTokenId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
