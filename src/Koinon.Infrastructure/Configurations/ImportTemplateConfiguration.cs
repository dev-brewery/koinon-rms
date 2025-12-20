using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ImportTemplate entity.
/// </summary>
public class ImportTemplateConfiguration : IEntityTypeConfiguration<ImportTemplate>
{
    public void Configure(EntityTypeBuilder<ImportTemplate> builder)
    {
        // Table name
        builder.ToTable("import_template");

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
            .HasDatabaseName("uix_import_template_guid");

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
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(e => e.ImportType)
            .HasColumnName("import_type")
            .IsRequired();

        builder.Property(e => e.FieldMappings)
            .HasColumnName("field_mappings")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsSystem)
            .HasColumnName("is_system")
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(e => e.ImportType)
            .HasDatabaseName("ix_import_template_import_type");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("ix_import_template_is_active");
    }
}
