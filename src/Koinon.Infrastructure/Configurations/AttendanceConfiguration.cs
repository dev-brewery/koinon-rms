using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Attendance entity.
/// </summary>
public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        // Table name
        builder.ToTable("attendance");

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
            .HasDatabaseName("uix_attendance_guid");

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
        builder.Property(e => e.OccurrenceId)
            .HasColumnName("occurrence_id")
            .IsRequired();

        builder.Property(e => e.PersonAliasId)
            .HasColumnName("person_alias_id");

        builder.Property(e => e.DeviceId)
            .HasColumnName("device_id");

        builder.Property(e => e.AttendanceCodeId)
            .HasColumnName("attendance_code_id");

        builder.Property(e => e.QualifierValueId)
            .HasColumnName("qualifier_value_id");

        builder.Property(e => e.CampusId)
            .HasColumnName("campus_id");

        builder.Property(e => e.PresentByPersonAliasId)
            .HasColumnName("present_by_person_alias_id");

        builder.Property(e => e.CheckedOutByPersonAliasId)
            .HasColumnName("checked_out_by_person_alias_id");

        builder.Property(e => e.DeclineReasonValueId)
            .HasColumnName("decline_reason_value_id");

        builder.Property(e => e.ScheduledByPersonAliasId)
            .HasColumnName("scheduled_by_person_alias_id");

        // Regular properties
        builder.Property(e => e.StartDateTime)
            .HasColumnName("start_date_time")
            .IsRequired();

        builder.Property(e => e.EndDateTime)
            .HasColumnName("end_date_time");

        builder.Property(e => e.RSVP)
            .HasColumnName("rsvp")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(Domain.Enums.RSVP.Unknown);

        builder.Property(e => e.DidAttend)
            .HasColumnName("did_attend");

        builder.Property(e => e.Note)
            .HasColumnName("note");

        builder.Property(e => e.ProcessedDateTime)
            .HasColumnName("processed_date_time");

        builder.Property(e => e.IsFirstTime)
            .HasColumnName("is_first_time")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.PresentDateTime)
            .HasColumnName("present_date_time");

        builder.Property(e => e.RequestedToAttend)
            .HasColumnName("requested_to_attend")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ScheduledToAttend)
            .HasColumnName("scheduled_to_attend")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ScheduleConfirmationSent)
            .HasColumnName("schedule_confirmation_sent")
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(e => e.Occurrence)
            .WithMany(o => o.Attendances)
            .HasForeignKey(e => e.OccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AttendanceCode)
            .WithMany(ac => ac.Attendances)
            .HasForeignKey(e => e.AttendanceCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(e => e.OccurrenceId)
            .HasDatabaseName("ix_attendance_occurrence_id");

        builder.HasIndex(e => e.PersonAliasId)
            .HasDatabaseName("ix_attendance_person_alias_id");

        builder.HasIndex(e => e.StartDateTime)
            .HasDatabaseName("ix_attendance_start_date_time");

        builder.HasIndex(e => e.DidAttend)
            .HasDatabaseName("ix_attendance_did_attend")
            .HasFilter("did_attend = true");

        builder.HasIndex(e => e.AttendanceCodeId)
            .HasDatabaseName("ix_attendance_code_id");
    }
}
