using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PagerAssignment entity.
/// </summary>
public class PagerAssignmentConfiguration : IEntityTypeConfiguration<PagerAssignment>
{
    public void Configure(EntityTypeBuilder<PagerAssignment> builder)
    {
        // Table name
        builder.ToTable("pager_assignment");

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
            .HasDatabaseName("uix_pager_assignment_guid");

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
        builder.Property(e => e.AttendanceId)
            .HasColumnName("attendance_id")
            .IsRequired();

        builder.Property(e => e.CampusId)
            .HasColumnName("campus_id");

        builder.Property(e => e.LocationId)
            .HasColumnName("location_id");

        // Pager number
        builder.Property(e => e.PagerNumber)
            .HasColumnName("pager_number")
            .IsRequired();

        // Relationships
        builder.HasOne(e => e.Attendance)
            .WithMany()
            .HasForeignKey(e => e.AttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Campus)
            .WithMany()
            .HasForeignKey(e => e.CampusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        // Unique index on attendance_id - one pager per attendance
        builder.HasIndex(e => e.AttendanceId)
            .IsUnique()
            .HasDatabaseName("uix_pager_assignment_attendance_id");

        // Index on pager_number for lookups
        builder.HasIndex(e => e.PagerNumber)
            .HasDatabaseName("ix_pager_assignment_pager_number");

        // Composite index for campus + date queries (for daily pager number uniqueness)
        builder.HasIndex(e => new { e.CampusId, e.CreatedDateTime })
            .HasDatabaseName("ix_pager_assignment_campus_id_created_date_time");
    }
}
