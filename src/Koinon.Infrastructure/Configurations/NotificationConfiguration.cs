using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Notification entity.
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        // Table name
        builder.ToTable("notification");

        // Primary key
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(n => n.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(n => n.Guid)
            .IsUnique()
            .HasDatabaseName("uix_notification_guid");

        // Ignore computed property
        builder.Ignore(n => n.IdKey);

        // Foreign key to Person
        builder.Property(n => n.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        // Index on person_id for efficient querying of user's notifications
        builder.HasIndex(n => n.PersonId)
            .HasDatabaseName("ix_notification_person_id");

        // Composite index for getting unread notifications by person
        builder.HasIndex(n => new { n.PersonId, n.IsRead })
            .HasDatabaseName("ix_notification_person_is_read");

        // Notification type (stored as int)
        builder.Property(n => n.NotificationType)
            .HasColumnName("notification_type")
            .HasConversion<int>()
            .IsRequired();

        // Title
        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        // Message
        builder.Property(n => n.Message)
            .HasColumnName("message")
            .HasMaxLength(1000)
            .IsRequired();

        // IsRead
        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        // ReadDateTime
        builder.Property(n => n.ReadDateTime)
            .HasColumnName("read_date_time");

        // ActionUrl
        builder.Property(n => n.ActionUrl)
            .HasColumnName("action_url")
            .HasMaxLength(500);

        // MetadataJson
        builder.Property(n => n.MetadataJson)
            .HasColumnName("metadata_json");

        // Audit fields
        builder.Property(n => n.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(n => n.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(n => n.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(n => n.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Navigation property
        builder.HasOne(n => n.Person)
            .WithMany()
            .HasForeignKey(n => n.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
