using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the GroupMemberRequest entity.
/// </summary>
public class GroupMemberRequestConfiguration : IEntityTypeConfiguration<GroupMemberRequest>
{
    public void Configure(EntityTypeBuilder<GroupMemberRequest> builder)
    {
        // Table name
        builder.ToTable("group_member_request");

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
            .HasDatabaseName("uix_group_member_request_guid");

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
        builder.Property(e => e.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(e => e.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(e => e.ProcessedByPersonId)
            .HasColumnName("processed_by_person_id");

        // Regular properties
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(Domain.Enums.GroupMemberRequestStatus.Pending);

        builder.Property(e => e.RequestNote)
            .HasColumnName("request_note")
            .HasMaxLength(2000);

        builder.Property(e => e.ResponseNote)
            .HasColumnName("response_note")
            .HasMaxLength(2000);

        builder.Property(e => e.ProcessedDateTime)
            .HasColumnName("processed_date_time");

        // Indexes for common queries
        // Composite index for finding pending requests by group (most common query)
        builder.HasIndex(e => new { e.GroupId, e.Status })
            .HasDatabaseName("ix_group_member_request_group_id_status");

        // Index for finding a person's requests
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_group_member_request_person_id");

        // Relationships
        builder.HasOne(e => e.Group)
            .WithMany(g => g.MemberRequests)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProcessedByPerson)
            .WithMany()
            .HasForeignKey(e => e.ProcessedByPersonId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
