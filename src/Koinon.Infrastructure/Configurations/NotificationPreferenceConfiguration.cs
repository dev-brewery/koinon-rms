using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the NotificationPreference entity.
/// </summary>
public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        // Table name
        builder.ToTable("notification_preference");

        // Primary key
        builder.HasKey(np => np.Id);
        builder.Property(np => np.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(np => np.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(np => np.Guid)
            .IsUnique()
            .HasDatabaseName("uix_notification_preference_guid");

        // Ignore computed property
        builder.Ignore(np => np.IdKey);

        // Foreign key to Person
        builder.Property(np => np.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        // Index on person_id
        builder.HasIndex(np => np.PersonId)
            .HasDatabaseName("ix_notification_preference_person_id");

        // Unique constraint on person_id + notification_type
        // Each user can only have one preference per notification type
        builder.HasIndex(np => new { np.PersonId, np.NotificationType })
            .IsUnique()
            .HasDatabaseName("uix_notification_preference_person_type");

        // Notification type (stored as int)
        builder.Property(np => np.NotificationType)
            .HasColumnName("notification_type")
            .HasConversion<int>()
            .IsRequired();

        // IsEnabled
        builder.Property(np => np.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        // Audit fields
        builder.Property(np => np.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(np => np.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(np => np.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(np => np.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Navigation property
        builder.HasOne(np => np.Person)
            .WithMany()
            .HasForeignKey(np => np.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
