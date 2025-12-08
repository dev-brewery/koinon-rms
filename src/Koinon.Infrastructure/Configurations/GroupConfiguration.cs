using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Group entity.
/// </summary>
public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        // Table name - "group" is a reserved SQL keyword, so it needs to be quoted
        builder.ToTable("group");

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
            .HasDatabaseName("uix_group_guid");

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
        builder.Property(e => e.IsSystem)
            .HasColumnName("is_system")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.GroupTypeId)
            .HasColumnName("group_type_id")
            .IsRequired();

        builder.Property(e => e.ParentGroupId)
            .HasColumnName("parent_group_id");

        builder.Property(e => e.CampusId)
            .HasColumnName("campus_id");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.IsSecurityRole)
            .HasColumnName("is_security_role")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ArchivedByPersonAliasId)
            .HasColumnName("archived_by_person_alias_id");

        builder.Property(e => e.ArchivedDateTime)
            .HasColumnName("archived_date_time");

        builder.Property(e => e.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.AllowGuests)
            .HasColumnName("allow_guests")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsPublic)
            .HasColumnName("is_public")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.GroupCapacity)
            .HasColumnName("group_capacity");

        builder.Property(e => e.ScheduleId)
            .HasColumnName("schedule_id");

        builder.Property(e => e.WelcomeSystemCommunicationId)
            .HasColumnName("welcome_system_communication_id");

        builder.Property(e => e.ExitSystemCommunicationId)
            .HasColumnName("exit_system_communication_id");

        builder.Property(e => e.RequiredSignatureDocumentTemplateId)
            .HasColumnName("required_signature_document_template_id");

        builder.Property(e => e.StatusValueId)
            .HasColumnName("status_value_id");

        builder.Property(e => e.MinAgeMonths)
            .HasColumnName("min_age_months");

        builder.Property(e => e.MaxAgeMonths)
            .HasColumnName("max_age_months");

        builder.Property(e => e.MinGrade)
            .HasColumnName("min_grade");

        builder.Property(e => e.MaxGrade)
            .HasColumnName("max_grade");

        // Indexes
        builder.HasIndex(e => e.GroupTypeId)
            .HasDatabaseName("ix_group_group_type_id");

        builder.HasIndex(e => e.ParentGroupId)
            .HasDatabaseName("ix_group_parent_group_id");

        builder.HasIndex(e => e.CampusId)
            .HasDatabaseName("ix_group_campus_id");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_group_name");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("ix_group_is_active")
            .HasFilter("is_active = true");

        builder.HasIndex(e => e.IsArchived)
            .HasDatabaseName("ix_group_is_archived")
            .HasFilter("is_archived = false");

        builder.HasIndex(e => e.ScheduleId)
            .HasDatabaseName("ix_group_schedule_id");

        // Relationships
        builder.HasOne(e => e.GroupType)
            .WithMany(gt => gt.Groups)
            .HasForeignKey(e => e.GroupTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Campus)
            .WithMany()
            .HasForeignKey(e => e.CampusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relationship for hierarchy
        builder.HasOne(e => e.ParentGroup)
            .WithMany(g => g.ChildGroups)
            .HasForeignKey(e => e.ParentGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Schedule relationship (added for check-in)
        builder.HasOne(e => e.Schedule)
            .WithMany(s => s.Groups)
            .HasForeignKey(e => e.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
