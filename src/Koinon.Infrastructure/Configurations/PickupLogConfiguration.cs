using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PickupLog entity.
/// </summary>
public class PickupLogConfiguration : IEntityTypeConfiguration<PickupLog>
{
    public void Configure(EntityTypeBuilder<PickupLog> builder)
    {
        // Table name
        builder.ToTable("pickup_log");

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
            .HasDatabaseName("uix_pickup_log_guid");

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

        builder.Property(e => e.ChildPersonId)
            .HasColumnName("child_person_id")
            .IsRequired();

        builder.Property(e => e.PickupPersonId)
            .HasColumnName("pickup_person_id");

        builder.Property(e => e.AuthorizedPickupId)
            .HasColumnName("authorized_pickup_id");

        builder.Property(e => e.SupervisorPersonId)
            .HasColumnName("supervisor_person_id");

        // Regular properties
        builder.Property(e => e.PickupPersonName)
            .HasColumnName("pickup_person_name")
            .HasMaxLength(200);

        builder.Property(e => e.WasAuthorized)
            .HasColumnName("was_authorized")
            .IsRequired();

        builder.Property(e => e.SupervisorOverride)
            .HasColumnName("supervisor_override")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CheckoutDateTime)
            .HasColumnName("checkout_date_time")
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasColumnName("notes");

        // Relationships
        builder.HasOne(e => e.Attendance)
            .WithMany()
            .HasForeignKey(e => e.AttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ChildPerson)
            .WithMany()
            .HasForeignKey(e => e.ChildPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PickupPerson)
            .WithMany()
            .HasForeignKey(e => e.PickupPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AuthorizedPickup)
            .WithMany(ap => ap.PickupLogs)
            .HasForeignKey(e => e.AuthorizedPickupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SupervisorPerson)
            .WithMany()
            .HasForeignKey(e => e.SupervisorPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(e => e.AttendanceId)
            .HasDatabaseName("ix_pickup_log_attendance_id");

        builder.HasIndex(e => e.ChildPersonId)
            .HasDatabaseName("ix_pickup_log_child_person_id");

        builder.HasIndex(e => e.CheckoutDateTime)
            .HasDatabaseName("ix_pickup_log_checkout_date_time");

        builder.HasIndex(e => e.PickupPersonId)
            .HasDatabaseName("ix_pickup_log_pickup_person_id");

        builder.HasIndex(e => e.AuthorizedPickupId)
            .HasDatabaseName("ix_pickup_log_authorized_pickup_id");

        builder.HasIndex(e => e.SupervisorPersonId)
            .HasDatabaseName("ix_pickup_log_supervisor_person_id");
    }
}
