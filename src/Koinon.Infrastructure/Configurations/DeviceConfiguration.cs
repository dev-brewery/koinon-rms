using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Device entity.
/// </summary>
public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        // Table name
        builder.ToTable("device");

        // Primary key
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique index
        builder.Property(d => d.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(d => d.Guid)
            .IsUnique()
            .HasDatabaseName("uix_device_guid");

        // Required fields
        builder.Property(d => d.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(d => d.DeviceTypeValueId)
            .HasColumnName("device_type_value_id");

        builder.Property(d => d.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45); // IPv6 max length

        builder.Property(d => d.PrinterSettings)
            .HasColumnName("printer_settings")
            .HasColumnType("jsonb");

        builder.Property(d => d.CampusId)
            .HasColumnName("campus_id");

        builder.Property(d => d.Locations)
            .HasColumnName("locations");

        builder.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Kiosk authentication fields
        builder.Property(d => d.KioskToken)
            .HasColumnName("kiosk_token")
            .HasMaxLength(128);

        builder.Property(d => d.KioskTokenExpiresAt)
            .HasColumnName("kiosk_token_expires_at");

        // Audit fields
        builder.Property(d => d.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(d => d.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(d => d.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(d => d.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Indexes
        builder.HasIndex(d => d.Name)
            .HasDatabaseName("ix_device_name");

        builder.HasIndex(d => d.IsActive)
            .HasDatabaseName("ix_device_is_active");

        builder.HasIndex(d => d.CampusId)
            .HasDatabaseName("ix_device_campus_id")
            .HasFilter("campus_id IS NOT NULL");

        // Unique index on kiosk_token (for fast lookups during authentication)
        builder.HasIndex(d => d.KioskToken)
            .IsUnique()
            .HasDatabaseName("uix_device_kiosk_token")
            .HasFilter("kiosk_token IS NOT NULL");

        // Foreign keys
        builder.HasOne(d => d.DeviceTypeValue)
            .WithMany()
            .HasForeignKey(d => d.DeviceTypeValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Campus)
            .WithMany()
            .HasForeignKey(d => d.CampusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(d => d.IdKey);
    }
}
