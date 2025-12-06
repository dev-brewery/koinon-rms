using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the DefinedType entity.
/// </summary>
public class DefinedTypeConfiguration : IEntityTypeConfiguration<DefinedType>
{
    public void Configure(EntityTypeBuilder<DefinedType> builder)
    {
        builder.ToTable("defined_type");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique constraint
        builder.Property(e => e.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(e => e.Guid)
            .IsUnique()
            .HasDatabaseName("uix_defined_type_guid");

        // IdKey is computed, not stored
        builder.Ignore(e => e.IdKey);

        // Core properties
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(100);

        builder.Property(e => e.HelpText)
            .HasColumnName("help_text");

        builder.Property(e => e.IsSystem)
            .HasColumnName("is_system")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Order)
            .HasColumnName("order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.FieldTypeAssemblyName)
            .HasColumnName("field_type_assembly_name")
            .HasMaxLength(200);

        // Audit properties
        builder.Property(e => e.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(e => e.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(e => e.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(e => e.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Indexes
        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ix_defined_type_name");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("ix_defined_type_category");

        builder.HasIndex(e => e.IsSystem)
            .HasDatabaseName("ix_defined_type_is_system");

        // Relationships
        builder.HasMany(e => e.DefinedValues)
            .WithOne(dv => dv.DefinedType)
            .HasForeignKey(dv => dv.DefinedTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
