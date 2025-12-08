using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the FollowUp entity.
/// </summary>
public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        // Table name
        builder.ToTable("follow_up");

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
            .HasDatabaseName("uix_follow_up_guid");

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
        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(e => e.AttendanceId)
            .HasColumnName("attendance_id");

        builder.Property(e => e.AssignedToPersonId)
            .HasColumnName("assigned_to_person_id");

        // Status
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(FollowUpStatus.Pending);

        // Notes
        builder.Property(e => e.Notes)
            .HasColumnName("notes");

        // Date/time fields
        builder.Property(e => e.ContactedDateTime)
            .HasColumnName("contacted_date_time");

        builder.Property(e => e.CompletedDateTime)
            .HasColumnName("completed_date_time");

        // Relationships
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Attendance)
            .WithMany()
            .HasForeignKey(e => e.AttendanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedToPerson)
            .WithMany()
            .HasForeignKey(e => e.AssignedToPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_follow_up_person_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_follow_up_status");

        builder.HasIndex(e => e.AssignedToPersonId)
            .HasDatabaseName("ix_follow_up_assigned_to_person_id")
            .HasFilter("assigned_to_person_id IS NOT NULL");

        builder.HasIndex(e => e.AttendanceId)
            .HasDatabaseName("ix_follow_up_attendance_id")
            .HasFilter("attendance_id IS NOT NULL");

        // Composite index for querying pending follow-ups by assigned person
        builder.HasIndex(e => new { e.AssignedToPersonId, e.Status })
            .HasDatabaseName("ix_follow_up_assigned_to_person_id_status")
            .HasFilter("assigned_to_person_id IS NOT NULL");
    }
}
