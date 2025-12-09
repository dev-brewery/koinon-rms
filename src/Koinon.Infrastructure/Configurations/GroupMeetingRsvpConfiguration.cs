using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the GroupMeetingRsvp entity.
/// </summary>
public class GroupMeetingRsvpConfiguration : IEntityTypeConfiguration<GroupMeetingRsvp>
{
    public void Configure(EntityTypeBuilder<GroupMeetingRsvp> builder)
    {
        // Table name
        builder.ToTable("group_meeting_rsvp");

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
            .HasDatabaseName("uix_group_meeting_rsvp_guid");

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
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        // Properties
        builder.Property(e => e.MeetingDate)
            .HasColumnName("meeting_date")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(RsvpStatus.NoResponse);

        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(e => e.RespondedDateTime)
            .HasColumnName("responded_date_time");

        // Unique constraint - one RSVP per person per meeting
        builder.HasIndex(e => new { e.GroupId, e.MeetingDate, e.PersonId })
            .IsUnique()
            .HasDatabaseName("uix_group_meeting_rsvp_group_meeting_person");

        // Indexes for common queries
        builder.HasIndex(e => new { e.GroupId, e.MeetingDate })
            .HasDatabaseName("ix_group_meeting_rsvp_group_id_meeting_date");

        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_group_meeting_rsvp_person_id");

        builder.HasIndex(e => e.MeetingDate)
            .HasDatabaseName("ix_group_meeting_rsvp_meeting_date");

        // Relationships
        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
