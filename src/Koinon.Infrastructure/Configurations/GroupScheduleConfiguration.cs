using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the GroupSchedule entity.
/// </summary>
public class GroupScheduleConfiguration : IEntityTypeConfiguration<GroupSchedule>
{
    public void Configure(EntityTypeBuilder<GroupSchedule> builder)
    {
        // Table name
        builder.ToTable("group_schedule");

        // Primary key
        builder.HasKey(gs => gs.Id);
        builder.Property(gs => gs.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique index
        builder.Property(gs => gs.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(gs => gs.Guid)
            .IsUnique()
            .HasDatabaseName("uix_group_schedule_guid");

        // IdKey is computed, not stored
        builder.Ignore(gs => gs.IdKey);

        // Audit fields
        builder.Property(gs => gs.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();
        builder.Property(gs => gs.ModifiedDateTime)
            .HasColumnName("modified_date_time");
        builder.Property(gs => gs.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");
        builder.Property(gs => gs.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Foreign keys
        builder.Property(gs => gs.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(gs => gs.ScheduleId)
            .HasColumnName("schedule_id")
            .IsRequired();

        builder.Property(gs => gs.LocationId)
            .HasColumnName("location_id");

        // Regular properties
        builder.Property(gs => gs.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        // Indexes
        // Unique constraint: a group can only have each schedule once
        builder.HasIndex(gs => new { gs.GroupId, gs.ScheduleId })
            .IsUnique()
            .HasDatabaseName("uix_group_schedule_group_id_schedule_id");

        // Index for querying schedules by group
        builder.HasIndex(gs => gs.GroupId)
            .HasDatabaseName("ix_group_schedule_group_id");

        // Index for querying groups by schedule
        builder.HasIndex(gs => gs.ScheduleId)
            .HasDatabaseName("ix_group_schedule_schedule_id");

        // Index for querying by location (optional FK)
        builder.HasIndex(gs => gs.LocationId)
            .HasDatabaseName("ix_group_schedule_location_id");

        // Relationships
        builder.HasOne(gs => gs.Group)
            .WithMany(g => g.GroupSchedules)
            .HasForeignKey(gs => gs.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gs => gs.Schedule)
            .WithMany(s => s.GroupSchedules)
            .HasForeignKey(gs => gs.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gs => gs.Location)
            .WithMany()
            .HasForeignKey(gs => gs.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
