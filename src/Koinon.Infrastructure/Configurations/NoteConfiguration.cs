using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Koinon.Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Note entity.
/// </summary>
public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        // Table name
        builder.ToTable("note");

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
            .HasDatabaseName("uix_note_guid");

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

        // Foreign keys
        builder.Property(e => e.PersonAliasId)
            .HasColumnName("person_alias_id")
            .IsRequired();

        builder.Property(e => e.NoteTypeValueId)
            .HasColumnName("note_type_value_id")
            .IsRequired();

        builder.Property(e => e.AuthorPersonAliasId)
            .HasColumnName("author_person_alias_id");

        // Note content
        builder.Property(e => e.Text)
            .HasColumnName("text")
            .IsRequired();

        // Date/time
        builder.Property(e => e.NoteDateTime)
            .HasColumnName("note_date_time")
            .IsRequired();

        // Flags
        builder.Property(e => e.IsPrivate)
            .HasColumnName("is_private")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.IsAlert)
            .HasColumnName("is_alert")
            .HasDefaultValue(false)
            .IsRequired();

        // Relationships
        builder.HasOne(e => e.PersonAlias)
            .WithMany()
            .HasForeignKey(e => e.PersonAliasId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.NoteTypeValue)
            .WithMany()
            .HasForeignKey(e => e.NoteTypeValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AuthorPersonAlias)
            .WithMany()
            .HasForeignKey(e => e.AuthorPersonAliasId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.PersonAliasId, e.NoteDateTime })
            .HasDatabaseName("ix_note_person_alias_id_note_date_time");

        builder.HasIndex(e => e.NoteTypeValueId)
            .HasDatabaseName("ix_note_note_type_value_id");
    }
}
