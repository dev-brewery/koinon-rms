using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PersonMergeHistory entity.
/// </summary>
public class PersonMergeHistoryConfiguration : IEntityTypeConfiguration<PersonMergeHistory>
{
    public void Configure(EntityTypeBuilder<PersonMergeHistory> builder)
    {
        // Table name
        builder.ToTable("person_merge_history");

        // Primary key
        builder.HasKey(pmh => pmh.Id);
        builder.Property(pmh => pmh.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(pmh => pmh.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(pmh => pmh.Guid)
            .IsUnique()
            .HasDatabaseName("uix_person_merge_history_guid");

        // Ignore computed property
        builder.Ignore(pmh => pmh.IdKey);

        // Foreign key: SurvivorPersonId
        builder.Property(pmh => pmh.SurvivorPersonId)
            .HasColumnName("survivor_person_id")
            .IsRequired();

        builder.HasOne(pmh => pmh.SurvivorPerson)
            .WithMany()
            .HasForeignKey(pmh => pmh.SurvivorPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pmh => pmh.SurvivorPersonId)
            .HasDatabaseName("ix_person_merge_history_survivor_person_id");

        // Foreign key: MergedPersonId
        builder.Property(pmh => pmh.MergedPersonId)
            .HasColumnName("merged_person_id")
            .IsRequired();

        builder.HasOne(pmh => pmh.MergedPerson)
            .WithMany()
            .HasForeignKey(pmh => pmh.MergedPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pmh => pmh.MergedPersonId)
            .HasDatabaseName("ix_person_merge_history_merged_person_id");

        // Foreign key: MergedByPersonId (nullable)
        builder.Property(pmh => pmh.MergedByPersonId)
            .HasColumnName("merged_by_person_id");

        builder.HasOne(pmh => pmh.MergedByPerson)
            .WithMany()
            .HasForeignKey(pmh => pmh.MergedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // MergedDateTime
        builder.Property(pmh => pmh.MergedDateTime)
            .HasColumnName("merged_date_time")
            .IsRequired();

        builder.HasIndex(pmh => pmh.MergedDateTime)
            .HasDatabaseName("ix_person_merge_history_merged_date_time");

        // Notes (optional text field)
        builder.Property(pmh => pmh.Notes)
            .HasColumnName("notes");

        // Audit fields
        builder.Property(pmh => pmh.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(pmh => pmh.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(pmh => pmh.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(pmh => pmh.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");
    }
}
