using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PagerMessage entity.
/// </summary>
public class PagerMessageConfiguration : IEntityTypeConfiguration<PagerMessage>
{
    public void Configure(EntityTypeBuilder<PagerMessage> builder)
    {
        // Table name
        builder.ToTable("pager_message");

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
            .HasDatabaseName("uix_pager_message_guid");

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
        builder.Property(e => e.PagerAssignmentId)
            .HasColumnName("pager_assignment_id")
            .IsRequired();

        builder.Property(e => e.SentByPersonId)
            .HasColumnName("sent_by_person_id")
            .IsRequired();

        // Enum properties stored as integers
        builder.Property(e => e.MessageType)
            .HasColumnName("message_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(PagerMessageStatus.Pending);

        // String properties
        builder.Property(e => e.MessageText)
            .HasColumnName("message_text")
            .IsRequired();

        builder.Property(e => e.PhoneNumber)
            .HasColumnName("phone_number")
            .IsRequired();

        builder.Property(e => e.TwilioMessageSid)
            .HasColumnName("twilio_message_sid");

        builder.Property(e => e.FailureReason)
            .HasColumnName("failure_reason");

        // Date/time fields
        builder.Property(e => e.SentDateTime)
            .HasColumnName("sent_date_time");

        builder.Property(e => e.DeliveredDateTime)
            .HasColumnName("delivered_date_time");

        // Relationships
        builder.HasOne(e => e.PagerAssignment)
            .WithMany(pa => pa.Messages)
            .HasForeignKey(e => e.PagerAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SentByPerson)
            .WithMany()
            .HasForeignKey(e => e.SentByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(e => e.PagerAssignmentId)
            .HasDatabaseName("ix_pager_message_pager_assignment_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_pager_message_status");

        builder.HasIndex(e => e.SentByPersonId)
            .HasDatabaseName("ix_pager_message_sent_by_person_id");

        // Index on Twilio SID for status callbacks
        builder.HasIndex(e => e.TwilioMessageSid)
            .HasDatabaseName("ix_pager_message_twilio_message_sid")
            .HasFilter("twilio_message_sid IS NOT NULL");
    }
}
