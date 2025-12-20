using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for ContributionDetail entity.
/// </summary>
public class ContributionDetailConfiguration : IEntityTypeConfiguration<ContributionDetail>
{
    public void Configure(EntityTypeBuilder<ContributionDetail> builder)
    {
        builder.ToTable("contribution_detail");

        // Primary key
        builder.HasKey(cd => cd.Id);
        builder.Property(cd => cd.Id).HasColumnName("id");

        // Guid/IdKey
        builder.Property(cd => cd.Guid).HasColumnName("guid").IsRequired();
        builder.HasIndex(cd => cd.Guid).IsUnique().HasDatabaseName("ix_contribution_detail_guid");

        // Audit fields
        builder.Property(cd => cd.CreatedDateTime).HasColumnName("created_date_time").IsRequired();
        builder.Property(cd => cd.ModifiedDateTime).HasColumnName("modified_date_time");
        builder.Property(cd => cd.CreatedByPersonAliasId).HasColumnName("created_by_person_alias_id");
        builder.Property(cd => cd.ModifiedByPersonAliasId).HasColumnName("modified_by_person_alias_id");

        // Business fields
        builder.Property(cd => cd.ContributionId).HasColumnName("contribution_id").IsRequired();

        builder.Property(cd => cd.FundId).HasColumnName("fund_id").IsRequired();

        builder.Property(cd => cd.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(cd => cd.Summary)
            .HasColumnName("summary")
            .HasColumnType("TEXT");

        // Relationships configured in ContributionConfiguration for the one-to-many
        builder.HasOne(cd => cd.Fund)
            .WithMany()
            .HasForeignKey(cd => cd.FundId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contribution_detail_fund");

        // Indexes
        builder.HasIndex(cd => cd.ContributionId)
            .HasDatabaseName("ix_contribution_detail_contribution");

        builder.HasIndex(cd => cd.FundId)
            .HasDatabaseName("ix_contribution_detail_fund");
    }
}
