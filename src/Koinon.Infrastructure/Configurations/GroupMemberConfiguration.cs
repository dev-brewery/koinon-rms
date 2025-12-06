using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the GroupMember entity.
/// </summary>
public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        // Table name
        builder.ToTable("group_member");

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
            .HasDatabaseName("uix_group_member_guid");

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

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(e => e.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(e => e.GroupRoleId)
            .HasColumnName("group_role_id")
            .IsRequired();

        builder.Property(e => e.GroupMemberStatus)
            .HasColumnName("group_member_status")
            .IsRequired()
            .HasDefaultValue(Domain.Enums.GroupMemberStatus.Active);

        builder.Property(e => e.DateTimeAdded)
            .HasColumnName("date_time_added");

        builder.Property(e => e.InactiveDateTime)
            .HasColumnName("inactive_date_time");

        builder.Property(e => e.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ArchivedDateTime)
            .HasColumnName("archived_date_time");

        builder.Property(e => e.ArchivedByPersonAliasId)
            .HasColumnName("archived_by_person_alias_id");

        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(e => e.IsNotified)
            .HasColumnName("is_notified")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CommunicationPreference)
            .HasColumnName("communication_preference");

        builder.Property(e => e.GuestCount)
            .HasColumnName("guest_count");

        // Indexes
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_group_member_person_id");

        builder.HasIndex(e => e.GroupId)
            .HasDatabaseName("ix_group_member_group_id");

        builder.HasIndex(e => e.GroupRoleId)
            .HasDatabaseName("ix_group_member_group_role_id");

        builder.HasIndex(e => e.GroupMemberStatus)
            .HasDatabaseName("ix_group_member_status");

        // Composite unique index on (GroupId, PersonId, GroupRoleId) for non-archived records
        // This ensures a person can only have one active membership in a group with a specific role
        builder.HasIndex(e => new { e.GroupId, e.PersonId, e.GroupRoleId })
            .IsUnique()
            .HasDatabaseName("uix_group_member_group_person_role")
            .HasFilter("is_archived = false");

        // Relationships
        builder.HasOne(e => e.Person)
            .WithMany(p => p.GroupMemberships)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.GroupRole)
            .WithMany()
            .HasForeignKey(e => e.GroupRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
