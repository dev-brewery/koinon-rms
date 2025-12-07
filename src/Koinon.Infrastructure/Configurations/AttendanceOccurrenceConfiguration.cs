using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the AttendanceOccurrence entity.
/// </summary>
public class AttendanceOccurrenceConfiguration : IEntityTypeConfiguration<AttendanceOccurrence>
{
    public void Configure(EntityTypeBuilder<AttendanceOccurrence> builder)
    {
        // Table name
        builder.ToTable("attendance_occurrence");

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
            .HasDatabaseName("uix_attendance_occurrence_guid");

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

        // Foreign keys
        builder.Property(e => e.GroupId)
            .HasColumnName("group_id");

        builder.Property(e => e.LocationId)
            .HasColumnName("location_id");

        builder.Property(e => e.ScheduleId)
            .HasColumnName("schedule_id");

        // Regular properties
        builder.Property(e => e.OccurrenceDate)
            .HasColumnName("occurrence_date")
            .IsRequired();

        builder.Property(e => e.DidNotOccur)
            .HasColumnName("did_not_occur");

        builder.Property(e => e.SundayDate)
            .HasColumnName("sunday_date")
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasColumnName("notes");

        builder.Property(e => e.AnonymousAttendanceCount)
            .HasColumnName("anonymous_attendance_count");

        builder.Property(e => e.AttendanceTypeValueId)
            .HasColumnName("attendance_type_value_id");

        builder.Property(e => e.DeclineConfirmationMessage)
            .HasColumnName("decline_confirmation_message");

        builder.Property(e => e.ShowDeclineReasons)
            .HasColumnName("show_decline_reasons")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AcceptConfirmationMessage)
            .HasColumnName("accept_confirmation_message");

        // Relationships
        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Schedule)
            .WithMany()
            .HasForeignKey(e => e.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one occurrence per group/location/schedule/date combination
        builder.HasIndex(e => new { e.GroupId, e.LocationId, e.ScheduleId, e.OccurrenceDate })
            .IsUnique()
            .HasDatabaseName("uix_attendance_occurrence_group_location_schedule_date");

        // Indexes for common queries
        builder.HasIndex(e => e.OccurrenceDate)
            .HasDatabaseName("ix_attendance_occurrence_date");

        builder.HasIndex(e => e.SundayDate)
            .HasDatabaseName("ix_attendance_occurrence_sunday_date");

        builder.HasIndex(e => e.GroupId)
            .HasDatabaseName("ix_attendance_occurrence_group_id");

        builder.HasIndex(e => e.LocationId)
            .HasDatabaseName("ix_attendance_occurrence_location_id");

        builder.HasIndex(e => e.ScheduleId)
            .HasDatabaseName("ix_attendance_occurrence_schedule_id");
    }
}
