using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for ContributionBatch entity.
/// </summary>
public class ContributionBatchConfiguration : IEntityTypeConfiguration<ContributionBatch>
{
    public void Configure(EntityTypeBuilder<ContributionBatch> builder)
    {
        builder.ToTable("contribution_batch");

        // Primary key
        builder.HasKey(cb => cb.Id);
        builder.Property(cb => cb.Id).HasColumnName("id");

        // Guid/IdKey
        builder.Property(cb => cb.Guid).HasColumnName("guid").IsRequired();
        builder.HasIndex(cb => cb.Guid).IsUnique().HasDatabaseName("ix_contribution_batch_guid");

        // Audit fields
        builder.Property(cb => cb.CreatedDateTime).HasColumnName("created_date_time").IsRequired();
        builder.Property(cb => cb.ModifiedDateTime).HasColumnName("modified_date_time");
        builder.Property(cb => cb.CreatedByPersonAliasId).HasColumnName("created_by_person_alias_id");
        builder.Property(cb => cb.ModifiedByPersonAliasId).HasColumnName("modified_by_person_alias_id");

        // Business fields
        builder.Property(cb => cb.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(cb => cb.BatchDate)
            .HasColumnName("batch_date")
            .IsRequired();

        builder.Property(cb => cb.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(cb => cb.ControlAmount)
            .HasColumnName("control_amount")
            .HasPrecision(18, 2);

        builder.Property(cb => cb.ControlItemCount)
            .HasColumnName("control_item_count");

        builder.Property(cb => cb.CampusId)
            .HasColumnName("campus_id");

        builder.Property(cb => cb.Note)
            .HasColumnName("note")
            .HasColumnType("TEXT");

        // Relationships
        builder.HasOne(cb => cb.Campus)
            .WithMany()
            .HasForeignKey(cb => cb.CampusId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_batch_campus");

        builder.HasMany(cb => cb.Contributions)
            .WithOne(c => c.Batch)
            .HasForeignKey(c => c.BatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_batch");

        // Indexes
        builder.HasIndex(cb => cb.Status)
            .HasDatabaseName("ix_contribution_batch_status");

        builder.HasIndex(cb => cb.BatchDate)
            .HasDatabaseName("ix_contribution_batch_batch_date");

        builder.HasIndex(cb => cb.CampusId)
            .HasDatabaseName("ix_contribution_batch_campus");
    }
}
