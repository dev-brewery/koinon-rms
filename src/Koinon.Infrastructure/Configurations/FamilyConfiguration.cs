using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Family entity.
/// </summary>
public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        // Table name
        builder.ToTable("family");

        // Primary key
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        // Guid with unique index
        builder.Property(f => f.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(f => f.Guid)
            .IsUnique()
            .HasDatabaseName("uix_family_guid");

        // Ignore computed properties
        builder.Ignore(f => f.IdKey);

        // Properties
        builder.Property(f => f.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(f => f.CampusId)
            .HasColumnName("campus_id");

        builder.Property(f => f.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields
        builder.Property(f => f.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(f => f.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        // Relationships
        builder.HasOne(f => f.Campus)
            .WithMany()
            .HasForeignKey(f => f.CampusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Members)
            .WithOne(fm => fm.Family)
            .HasForeignKey(fm => fm.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(f => f.CampusId)
            .HasDatabaseName("ix_family_campus_id");

        builder.HasIndex(f => f.Name)
            .HasDatabaseName("ix_family_name");
    }
}
