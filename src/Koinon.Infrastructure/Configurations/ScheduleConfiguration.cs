using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Schedule entity.
/// </summary>
public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        // Table name
        builder.ToTable("schedule");

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
            .HasDatabaseName("uix_schedule_guid");

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
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.ICalendarContent)
            .HasColumnName("icalendar_content");

        builder.Property(e => e.CheckInStartOffsetMinutes)
            .HasColumnName("check_in_start_offset_minutes");

        builder.Property(e => e.CheckInEndOffsetMinutes)
            .HasColumnName("check_in_end_offset_minutes");

        builder.Property(e => e.EffectiveStartDate)
            .HasColumnName("effective_start_date");

        builder.Property(e => e.EffectiveEndDate)
            .HasColumnName("effective_end_date");

        builder.Property(e => e.CategoryId)
            .HasColumnName("category_id");

        builder.Property(e => e.WeeklyDayOfWeek)
            .HasColumnName("weekly_day_of_week")
            .HasConversion<int?>();

        builder.Property(e => e.WeeklyTimeOfDay)
            .HasColumnName("weekly_time_of_day");

        builder.Property(e => e.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.AutoInactivateWhenComplete)
            .HasColumnName("auto_inactivate_when_complete")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsPublic)
            .HasColumnName("is_public")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_schedule_name");

        builder.HasIndex(e => new { e.WeeklyDayOfWeek, e.WeeklyTimeOfDay })
            .HasDatabaseName("ix_schedule_weekly");
    }
}
