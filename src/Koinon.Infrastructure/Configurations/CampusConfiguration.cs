using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Campus entity.
/// </summary>
public class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        // Table name
        builder.ToTable("campus");

        // Primary key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique index
        builder.Property(c => c.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(c => c.Guid)
            .IsUnique()
            .HasDatabaseName("uix_campus_guid");

        // Required fields
        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.ShortCode)
            .HasColumnName("short_code")
            .HasMaxLength(10);

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.Url)
            .HasColumnName("url")
            .HasMaxLength(200);

        builder.Property(c => c.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20);

        builder.Property(c => c.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(50);

        builder.Property(c => c.CampusStatusValueId)
            .HasColumnName("campus_status_value_id");

        builder.Property(c => c.LeaderPersonAliasId)
            .HasColumnName("leader_person_alias_id");

        builder.Property(c => c.ServiceTimes)
            .HasColumnName("service_times");

        builder.Property(c => c.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        // Audit fields
        builder.Property(c => c.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(c => c.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(c => c.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(c => c.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Indexes
        builder.HasIndex(c => c.ShortCode)
            .HasDatabaseName("ix_campus_short_code")
            .HasFilter("short_code IS NOT NULL");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("ix_campus_is_active");

        builder.HasIndex(c => c.Order)
            .HasDatabaseName("ix_campus_order");

        // Foreign keys
        builder.HasOne(c => c.CampusStatusValue)
            .WithMany()
            .HasForeignKey(c => c.CampusStatusValueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(c => c.IdKey);
    }
}
