using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Location entity.
/// </summary>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        // Table name
        builder.ToTable("location");

        // Primary key
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique index
        builder.Property(l => l.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(l => l.Guid)
            .IsUnique()
            .HasDatabaseName("uix_location_guid");

        // Self-referencing foreign key for parent location
        builder.Property(l => l.ParentLocationId)
            .HasColumnName("parent_location_id");

        // Required fields
        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.LocationTypeValueId)
            .HasColumnName("location_type_value_id");

        builder.Property(l => l.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(l => l.PrinterDeviceId)
            .HasColumnName("printer_device_id");

        builder.Property(l => l.ImageId)
            .HasColumnName("image_id");

        builder.Property(l => l.SoftRoomThreshold)
            .HasColumnName("soft_room_threshold");

        builder.Property(l => l.FirmRoomThreshold)
            .HasColumnName("firm_room_threshold");

        builder.Property(l => l.StaffToChildRatio)
            .HasColumnName("staff_to_child_ratio");

        builder.Property(l => l.OverflowLocationId)
            .HasColumnName("overflow_location_id");

        builder.Property(l => l.AutoAssignOverflow)
            .HasColumnName("auto_assign_overflow")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(l => l.IsGeoPointLocked)
            .HasColumnName("is_geo_point_locked")
            .IsRequired()
            .HasDefaultValue(false);

        // Address properties
        builder.Property(l => l.Street1)
            .HasColumnName("street1")
            .HasMaxLength(100);

        builder.Property(l => l.Street2)
            .HasColumnName("street2")
            .HasMaxLength(100);

        builder.Property(l => l.City)
            .HasColumnName("city")
            .HasMaxLength(50);

        builder.Property(l => l.State)
            .HasColumnName("state")
            .HasMaxLength(50);

        builder.Property(l => l.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(20);

        builder.Property(l => l.Country)
            .HasColumnName("country")
            .HasMaxLength(50);

        // Geographic coordinates
        builder.Property(l => l.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(9, 6); // Standard latitude precision

        builder.Property(l => l.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(9, 6); // Standard longitude precision

        builder.Property(l => l.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        // Audit fields
        builder.Property(l => l.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(l => l.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(l => l.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(l => l.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Indexes
        builder.HasIndex(l => l.ParentLocationId)
            .HasDatabaseName("ix_location_parent_location_id");

        builder.HasIndex(l => l.IsActive)
            .HasDatabaseName("ix_location_is_active");

        builder.HasIndex(l => l.LocationTypeValueId)
            .HasDatabaseName("ix_location_location_type_value_id");

        builder.HasIndex(l => l.Name)
            .HasDatabaseName("ix_location_name");

        builder.HasIndex(l => l.OverflowLocationId)
            .HasDatabaseName("ix_location_overflow_location_id");

        // Foreign key relationships
        builder.HasOne(l => l.ParentLocation)
            .WithMany(l => l.ChildLocations)
            .HasForeignKey(l => l.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.LocationTypeValue)
            .WithMany()
            .HasForeignKey(l => l.LocationTypeValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.OverflowLocation)
            .WithMany()
            .HasForeignKey(l => l.OverflowLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(l => l.IdKey);
    }
}
