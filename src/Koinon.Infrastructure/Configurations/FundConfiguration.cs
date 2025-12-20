using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for Fund entity.
/// </summary>
public class FundConfiguration : IEntityTypeConfiguration<Fund>
{
    public void Configure(EntityTypeBuilder<Fund> builder)
    {
        builder.ToTable("fund");

        // Primary key
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        // Guid/IdKey
        builder.Property(f => f.Guid).HasColumnName("guid").IsRequired();
        builder.HasIndex(f => f.Guid).IsUnique().HasDatabaseName("ix_fund_guid");

        // Audit fields
        builder.Property(f => f.CreatedDateTime).HasColumnName("created_date_time").IsRequired();
        builder.Property(f => f.ModifiedDateTime).HasColumnName("modified_date_time");
        builder.Property(f => f.CreatedByPersonAliasId).HasColumnName("created_by_person_alias_id");
        builder.Property(f => f.ModifiedByPersonAliasId).HasColumnName("modified_by_person_alias_id");

        // Business fields
        builder.Property(f => f.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.PublicName)
            .HasColumnName("public_name")
            .HasMaxLength(100);

        builder.Property(f => f.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT");

        builder.Property(f => f.GlCode)
            .HasColumnName("gl_code")
            .HasMaxLength(50);

        builder.Property(f => f.IsTaxDeductible)
            .HasColumnName("is_tax_deductible")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.IsPublic)
            .HasColumnName("is_public")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.StartDate)
            .HasColumnName("start_date");

        builder.Property(f => f.EndDate)
            .HasColumnName("end_date");

        builder.Property(f => f.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(f => f.ParentFundId)
            .HasColumnName("parent_fund_id");

        builder.Property(f => f.CampusId)
            .HasColumnName("campus_id");

        // Self-referential relationship
        builder.HasOne(f => f.ParentFund)
            .WithMany(f => f.ChildFunds)
            .HasForeignKey(f => f.ParentFundId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fund_parent_fund");

        // Campus relationship
        builder.HasOne(f => f.Campus)
            .WithMany()
            .HasForeignKey(f => f.CampusId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fund_campus");

        // Indexes
        builder.HasIndex(f => f.ParentFundId)
            .HasDatabaseName("ix_fund_parent");

        builder.HasIndex(f => f.CampusId)
            .HasDatabaseName("ix_fund_campus");

        builder.HasIndex(f => f.IsActive)
            .HasDatabaseName("ix_fund_active")
            .HasFilter("is_active = true");
    }
}
