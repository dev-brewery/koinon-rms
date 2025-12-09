using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Communication entity.
/// </summary>
public class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> builder)
    {
        // Table name
        builder.ToTable("communication");

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
            .HasDatabaseName("uix_communication_guid");

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
        builder.Property(e => e.CommunicationType)
            .HasColumnName("communication_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(CommunicationStatus.Draft);

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500);

        builder.Property(e => e.Body)
            .HasColumnName("body")
            .IsRequired();

        builder.Property(e => e.FromEmail)
            .HasColumnName("from_email")
            .HasMaxLength(254);

        builder.Property(e => e.FromName)
            .HasColumnName("from_name")
            .HasMaxLength(200);

        builder.Property(e => e.ReplyToEmail)
            .HasColumnName("reply_to_email")
            .HasMaxLength(254);

        builder.Property(e => e.SentDateTime)
            .HasColumnName("sent_date_time");

        builder.Property(e => e.RecipientCount)
            .HasColumnName("recipient_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.DeliveredCount)
            .HasColumnName("delivered_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.FailedCount)
            .HasColumnName("failed_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.OpenedCount)
            .HasColumnName("opened_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ClickedCount)
            .HasColumnName("clicked_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_communication_status");

        builder.HasIndex(e => e.CommunicationType)
            .HasDatabaseName("ix_communication_communication_type");

        builder.HasIndex(e => e.CreatedDateTime)
            .HasDatabaseName("ix_communication_created_date_time");

        builder.HasIndex(e => e.SentDateTime)
            .HasDatabaseName("ix_communication_sent_date_time");

        builder.HasIndex(e => e.CreatedByPersonAliasId)
            .HasDatabaseName("ix_communication_created_by_person_alias_id");
    }
}
