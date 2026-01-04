using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the UserPreference entity.
/// </summary>
public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        // Table name
        builder.ToTable("user_preference");

        // Primary key
        builder.HasKey(up => up.Id);
        builder.Property(up => up.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(up => up.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(up => up.Guid)
            .IsUnique()
            .HasDatabaseName("uix_user_preference_guid");

        // Ignore computed property
        builder.Ignore(up => up.IdKey);

        // Foreign key to Person
        builder.Property(up => up.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.HasIndex(up => up.PersonId)
            .IsUnique()
            .HasDatabaseName("uix_user_preference_person_id");

        // Theme (stored as int)
        builder.Property(up => up.Theme)
            .HasColumnName("theme")
            .HasConversion<int>()
            .IsRequired();

        // Date format
        builder.Property(up => up.DateFormat)
            .HasColumnName("date_format")
            .HasMaxLength(20)
            .IsRequired();

        // Timezone
        builder.Property(up => up.TimeZone)
            .HasColumnName("time_zone")
            .HasMaxLength(64)
            .IsRequired();

        // Audit fields
        builder.Property(up => up.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(up => up.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(up => up.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(up => up.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Navigation property
        builder.HasOne(up => up.Person)
            .WithMany()
            .HasForeignKey(up => up.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
