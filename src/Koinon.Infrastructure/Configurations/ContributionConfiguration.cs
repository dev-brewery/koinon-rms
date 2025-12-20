using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for Contribution entity.
/// </summary>
public class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.ToTable("contribution");

        // Primary key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        // Guid/IdKey
        builder.Property(c => c.Guid).HasColumnName("guid").IsRequired();
        builder.HasIndex(c => c.Guid).IsUnique().HasDatabaseName("ix_contribution_guid");

        // Audit fields
        builder.Property(c => c.CreatedDateTime).HasColumnName("created_date_time").IsRequired();
        builder.Property(c => c.ModifiedDateTime).HasColumnName("modified_date_time");
        builder.Property(c => c.CreatedByPersonAliasId).HasColumnName("created_by_person_alias_id");
        builder.Property(c => c.ModifiedByPersonAliasId).HasColumnName("modified_by_person_alias_id");

        // Business fields
        builder.Property(c => c.PersonAliasId).HasColumnName("person_alias_id");

        builder.Property(c => c.BatchId).HasColumnName("batch_id");

        builder.Property(c => c.TransactionDateTime).HasColumnName("transaction_date_time").IsRequired();

        builder.Property(c => c.TransactionCode)
            .HasColumnName("transaction_code")
            .HasMaxLength(50);

        builder.Property(c => c.TransactionTypeValueId).HasColumnName("transaction_type_value_id").IsRequired();

        builder.Property(c => c.SourceTypeValueId).HasColumnName("source_type_value_id").IsRequired();

        builder.Property(c => c.Summary)
            .HasColumnName("summary")
            .HasColumnType("TEXT");

        builder.Property(c => c.CampusId).HasColumnName("campus_id");

        // Relationships
        builder.HasOne(c => c.PersonAlias)
            .WithMany()
            .HasForeignKey(c => c.PersonAliasId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_person_alias");

        builder.HasOne(c => c.TransactionTypeValue)
            .WithMany()
            .HasForeignKey(c => c.TransactionTypeValueId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_transaction_type");

        builder.HasOne(c => c.SourceTypeValue)
            .WithMany()
            .HasForeignKey(c => c.SourceTypeValueId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_source_type");

        builder.HasOne(c => c.Campus)
            .WithMany()
            .HasForeignKey(c => c.CampusId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_campus");

        builder.HasOne(c => c.Batch)
            .WithMany(cb => cb.Contributions)
            .HasForeignKey(c => c.BatchId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_batch");

        builder.HasMany(c => c.ContributionDetails)
            .WithOne(cd => cd.Contribution)
            .HasForeignKey(cd => cd.ContributionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_contribution_detail_contribution");

        // Indexes
        builder.HasIndex(c => c.PersonAliasId)
            .HasDatabaseName("ix_contribution_person_alias");

        builder.HasIndex(c => c.BatchId)
            .HasDatabaseName("ix_contribution_batch");

        builder.HasIndex(c => c.TransactionDateTime)
            .HasDatabaseName("ix_contribution_transaction_date");

        builder.HasIndex(c => c.TransactionTypeValueId)
            .HasDatabaseName("ix_contribution_transaction_type");

        builder.HasIndex(c => c.SourceTypeValueId)
            .HasDatabaseName("ix_contribution_source_type");

        builder.HasIndex(c => c.CampusId)
            .HasDatabaseName("ix_contribution_campus");
    }
}
