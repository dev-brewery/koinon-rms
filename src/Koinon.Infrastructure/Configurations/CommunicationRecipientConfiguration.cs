using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the CommunicationRecipient entity.
/// </summary>
public class CommunicationRecipientConfiguration : IEntityTypeConfiguration<CommunicationRecipient>
{
    public void Configure(EntityTypeBuilder<CommunicationRecipient> builder)
    {
        // Table name
        builder.ToTable("communication_recipient");

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
            .HasDatabaseName("uix_communication_recipient_guid");

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
        builder.Property(e => e.CommunicationId)
            .HasColumnName("communication_id")
            .IsRequired();

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(e => e.Address)
            .HasColumnName("address")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(e => e.RecipientName)
            .HasColumnName("recipient_name")
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(CommunicationRecipientStatus.Pending);

        builder.Property(e => e.DeliveredDateTime)
            .HasColumnName("delivered_date_time");

        builder.Property(e => e.OpenedDateTime)
            .HasColumnName("opened_date_time");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(1000);

        builder.Property(e => e.ExternalMessageId)
            .HasColumnName("external_message_id")
            .HasMaxLength(64);

        builder.Property(e => e.ErrorCode)
            .HasColumnName("error_code");

        builder.Property(e => e.GroupId)
            .HasColumnName("group_id");

        // Indexes
        builder.HasIndex(e => e.CommunicationId)
            .HasDatabaseName("ix_communication_recipient_communication_id");

        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_communication_recipient_person_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_communication_recipient_status");

        builder.HasIndex(e => e.GroupId)
            .HasDatabaseName("ix_communication_recipient_group_id");

        builder.HasIndex(e => e.ExternalMessageId)
            .HasDatabaseName("ix_communication_recipient_external_message_id");

        // Relationships
        builder.HasOne(e => e.Communication)
            .WithMany(c => c.Recipients)
            .HasForeignKey(e => e.CommunicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
