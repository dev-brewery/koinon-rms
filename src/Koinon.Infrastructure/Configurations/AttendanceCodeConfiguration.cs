using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the AttendanceCode entity.
/// </summary>
public class AttendanceCodeConfiguration : IEntityTypeConfiguration<AttendanceCode>
{
    public void Configure(EntityTypeBuilder<AttendanceCode> builder)
    {
        // Table name
        builder.ToTable("attendance_code");

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
            .HasDatabaseName("uix_attendance_code_guid");

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
        builder.Property(e => e.IssueDateTime)
            .HasColumnName("issue_date_time")
            .IsRequired();

        builder.Property(e => e.IssueDate)
            .HasColumnName("issue_date")
            .IsRequired();

        builder.Property(e => e.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        // Unique constraint: codes must be unique per day (not per timestamp)
        builder.HasIndex(e => new { e.IssueDate, e.Code })
            .IsUnique()
            .HasDatabaseName("uix_attendance_code_issue_date_code");

        // Index for lookups
        builder.HasIndex(e => e.Code)
            .HasDatabaseName("ix_attendance_code_code");
    }
}
