using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for ContributionStatement entity.
/// </summary>
public class ContributionStatementConfiguration : IEntityTypeConfiguration<ContributionStatement>
{
    public void Configure(EntityTypeBuilder<ContributionStatement> builder)
    {
        builder.ToTable("contribution_statement");

        // Primary key
        builder.HasKey(cs => cs.Id);
        builder.Property(cs => cs.Id).HasColumnName("id");

        // Guid/IdKey
        builder.Property(cs => cs.Guid).HasColumnName("guid").IsRequired();
        builder.HasIndex(cs => cs.Guid).IsUnique().HasDatabaseName("ix_contribution_statement_guid");

        // Audit fields
        builder.Property(cs => cs.CreatedDateTime).HasColumnName("created_date_time").IsRequired();
        builder.Property(cs => cs.ModifiedDateTime).HasColumnName("modified_date_time");
        builder.Property(cs => cs.CreatedByPersonAliasId).HasColumnName("created_by_person_alias_id");
        builder.Property(cs => cs.ModifiedByPersonAliasId).HasColumnName("modified_by_person_alias_id");

        // Business fields
        builder.Property(cs => cs.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(cs => cs.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(cs => cs.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(cs => cs.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(cs => cs.ContributionCount)
            .HasColumnName("contribution_count")
            .IsRequired();

        builder.Property(cs => cs.GeneratedDateTime)
            .HasColumnName("generated_date_time")
            .IsRequired();

        builder.Property(cs => cs.BinaryFileId)
            .HasColumnName("binary_file_id");

        // Relationships
        builder.HasOne(cs => cs.Person)
            .WithMany()
            .HasForeignKey(cs => cs.PersonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_statement_person");

        builder.HasOne(cs => cs.BinaryFile)
            .WithMany()
            .HasForeignKey(cs => cs.BinaryFileId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_contribution_statement_binary_file");

        // Indexes
        builder.HasIndex(cs => cs.PersonId)
            .HasDatabaseName("ix_contribution_statement_person");

        builder.HasIndex(cs => new { cs.PersonId, cs.StartDate, cs.EndDate })
            .HasDatabaseName("ix_contribution_statement_person_period");
    }
}
