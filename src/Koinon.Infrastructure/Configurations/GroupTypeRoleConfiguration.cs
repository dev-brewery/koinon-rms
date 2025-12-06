using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the GroupTypeRole entity.
/// </summary>
public class GroupTypeRoleConfiguration : IEntityTypeConfiguration<GroupTypeRole>
{
    public void Configure(EntityTypeBuilder<GroupTypeRole> builder)
    {
        // Table name
        builder.ToTable("group_type_role");

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
            .HasDatabaseName("uix_group_type_role_guid");

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

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.IsLeader)
            .HasColumnName("is_leader")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CanView)
            .HasColumnName("can_view")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CanEdit)
            .HasColumnName("can_edit")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CanManageMembers)
            .HasColumnName("can_manage_members")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ReceiveRequirementsNotifications)
            .HasColumnName("receive_requirements_notifications")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.MaxCount)
            .HasColumnName("max_count");

        builder.Property(e => e.MinCount)
            .HasColumnName("min_count");

        builder.Property(e => e.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(e => e.GroupTypeId)
            .HasDatabaseName("ix_group_type_role_group_type_id");

        builder.HasIndex(e => new { e.GroupTypeId, e.Name })
            .HasDatabaseName("ix_group_type_role_group_type_id_name");

        // Relationships configured in GroupTypeConfiguration (cascade delete)
    }
}
