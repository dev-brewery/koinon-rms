using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the GroupType entity.
/// </summary>
public class GroupTypeConfiguration : IEntityTypeConfiguration<GroupType>
{
    public void Configure(EntityTypeBuilder<GroupType> builder)
    {
        // Table name
        builder.ToTable("group_type");

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
            .HasDatabaseName("uix_group_type_guid");

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

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.GroupTerm)
            .HasColumnName("group_term")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("Group");

        builder.Property(e => e.GroupMemberTerm)
            .HasColumnName("group_member_term")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("Member");

        builder.Property(e => e.DefaultGroupRoleId)
            .HasColumnName("default_group_role_id");

        builder.Property(e => e.IconCssClass)
            .HasColumnName("icon_css_class")
            .HasMaxLength(100);

        builder.Property(e => e.Color)
            .HasColumnName("color")
            .HasMaxLength(7);

        builder.Property(e => e.DefaultIsPublic)
            .HasColumnName("default_is_public")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AllowSelfRegistration)
            .HasColumnName("allow_self_registration")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.RequiresMemberApproval)
            .HasColumnName("requires_member_approval")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.DefaultGroupCapacity)
            .HasColumnName("default_group_capacity");

        builder.Property(e => e.AllowMultipleLocations)
            .HasColumnName("allow_multiple_locations")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ShowInGroupList)
            .HasColumnName("show_in_group_list")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ShowInNavigation)
            .HasColumnName("show_in_navigation")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.TakesAttendance)
            .HasColumnName("takes_attendance")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AttendanceCountsAsWeekendService)
            .HasColumnName("attendance_counts_as_weekend_service")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.SendAttendanceReminder)
            .HasColumnName("send_attendance_reminder")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ShowConnectionStatus)
            .HasColumnName("show_connection_status")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.EnableSpecificGroupRequirements)
            .HasColumnName("enable_specific_group_requirements")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AllowGroupSync)
            .HasColumnName("allow_group_sync")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AllowSpecificGroupMemberAttributes)
            .HasColumnName("allow_specific_group_member_attributes")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.GroupTypePurposeValueId)
            .HasColumnName("group_type_purpose_value_id");

        builder.Property(e => e.IgnorePersonInactivated)
            .HasColumnName("ignore_person_inactivated")
            .IsRequired()
            .HasDefaultValue(false);

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

        // Indexes
        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_group_type_name");

        // Relationships
        builder.HasMany(e => e.Roles)
            .WithOne(r => r.GroupType)
            .HasForeignKey(r => r.GroupTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Groups)
            .WithOne(g => g.GroupType)
            .HasForeignKey(g => g.GroupTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
