using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the PersonNote entity.
/// </summary>
public class PersonNoteConfiguration : IEntityTypeConfiguration<PersonNote>
{
    public void Configure(EntityTypeBuilder<PersonNote> builder)
    {
        // Table name
        builder.ToTable("person_note");

        // Primary key
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Guid with unique index
        builder.Property(n => n.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(n => n.Guid)
            .IsUnique()
            .HasDatabaseName("uix_person_note_guid");

        // Required fields
        builder.Property(n => n.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(n => n.Text)
            .HasColumnName("text")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(n => n.NoteDate)
            .HasColumnName("note_date")
            .IsRequired();

        builder.Property(n => n.NoteTypeDefinedValueId)
            .HasColumnName("note_type_defined_value_id");

        builder.Property(n => n.IsPrivate)
            .HasColumnName("is_private")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.IsAlert)
            .HasColumnName("is_alert")
            .IsRequired()
            .HasDefaultValue(false);

        // Audit fields (inherited from Entity)
        builder.Property(n => n.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(n => n.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(n => n.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(n => n.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");

        // Indexes for performance
        builder.HasIndex(n => n.PersonId)
            .HasDatabaseName("ix_person_note_person_id");

        builder.HasIndex(n => n.NoteDate)
            .HasDatabaseName("ix_person_note_note_date");

        builder.HasIndex(n => n.NoteTypeDefinedValueId)
            .HasDatabaseName("ix_person_note_note_type_defined_value_id")
            .HasFilter("note_type_defined_value_id IS NOT NULL");

        // Foreign keys
        builder.HasOne(n => n.Person)
            .WithMany()
            .HasForeignKey(n => n.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.NoteTypeDefinedValue)
            .WithMany()
            .HasForeignKey(n => n.NoteTypeDefinedValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.CreatedByPersonAlias)
            .WithMany()
            .HasForeignKey(n => n.CreatedByPersonAliasId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore computed properties
        builder.Ignore(n => n.IdKey);
    }
}
