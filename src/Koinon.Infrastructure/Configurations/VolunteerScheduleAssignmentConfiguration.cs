using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the VolunteerScheduleAssignment entity.
/// </summary>
public class VolunteerScheduleAssignmentConfiguration : IEntityTypeConfiguration<VolunteerScheduleAssignment>
{
    public void Configure(EntityTypeBuilder<VolunteerScheduleAssignment> builder)
    {
        // Table name
        builder.ToTable("volunteer_schedule_assignment");

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
            .HasDatabaseName("uix_volunteer_schedule_assignment_guid");

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
        builder.Property(e => e.GroupMemberId)
            .HasColumnName("group_member_id")
            .IsRequired();

        builder.Property(e => e.ScheduleId)
            .HasColumnName("schedule_id")
            .IsRequired();

        builder.Property(e => e.AssignedDate)
            .HasColumnName("assigned_date")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasDefaultValue(VolunteerScheduleStatus.Scheduled);

        builder.Property(e => e.DeclineReason)
            .HasColumnName("decline_reason")
            .HasMaxLength(500);

        builder.Property(e => e.RespondedDateTime)
            .HasColumnName("responded_date_time");

        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        // Unique constraint to prevent double-booking
        // A volunteer (GroupMember) cannot be assigned to the same Schedule on the same Date more than once
        builder.HasIndex(e => new { e.GroupMemberId, e.ScheduleId, e.AssignedDate })
            .IsUnique()
            .HasDatabaseName("uix_volunteer_schedule_assignment_member_schedule_date");

        // Indexes for common queries
        builder.HasIndex(e => e.GroupMemberId)
            .HasDatabaseName("ix_volunteer_schedule_assignment_group_member_id");

        builder.HasIndex(e => e.ScheduleId)
            .HasDatabaseName("ix_volunteer_schedule_assignment_schedule_id");

        builder.HasIndex(e => e.AssignedDate)
            .HasDatabaseName("ix_volunteer_schedule_assignment_assigned_date");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_volunteer_schedule_assignment_status");

        // Relationships
        builder.HasOne(e => e.GroupMember)
            .WithMany()
            .HasForeignKey(e => e.GroupMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Schedule)
            .WithMany()
            .HasForeignKey(e => e.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
