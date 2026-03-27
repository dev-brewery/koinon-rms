namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a pastoral note or interaction log entry on a person's record.
/// Used for tracking pastoral care, prayer requests, counseling visits, and general notes.
/// </summary>
public class Note : Entity
{
    /// <summary>
    /// Foreign key to the PersonAlias this note is about.
    /// </summary>
    public required int PersonAliasId { get; set; }

    /// <summary>
    /// Foreign key to the DefinedValue for the note type
    /// (e.g., General, Prayer Request, Pastoral Visit, Counseling).
    /// </summary>
    public required int NoteTypeValueId { get; set; }

    /// <summary>
    /// The text content of the note.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// The date and time when the interaction or event occurred.
    /// </summary>
    public required DateTime NoteDateTime { get; set; }

    /// <summary>
    /// Optional foreign key to the PersonAlias of the staff member who authored this note.
    /// Null when the authoring user cannot be determined from the auth context.
    /// </summary>
    public int? AuthorPersonAliasId { get; set; }

    /// <summary>
    /// When true, this note is only visible to the author.
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// When true, this note should be displayed prominently on the person's profile.
    /// </summary>
    public bool IsAlert { get; set; }

    // Navigation properties

    /// <summary>
    /// The PersonAlias this note is about.
    /// </summary>
    public virtual PersonAlias? PersonAlias { get; set; }

    /// <summary>
    /// The DefinedValue representing the note type.
    /// </summary>
    public virtual DefinedValue? NoteTypeValue { get; set; }

    /// <summary>
    /// The PersonAlias of the staff member who authored this note.
    /// </summary>
    public virtual PersonAlias? AuthorPersonAlias { get; set; }
}
