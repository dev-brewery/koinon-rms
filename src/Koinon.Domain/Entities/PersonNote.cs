namespace Koinon.Domain.Entities;

/// <summary>
/// A note or interaction log entry on a person record.
/// </summary>
public class PersonNote : Entity
{
    /// <summary>Person this note belongs to.</summary>
    public int PersonId { get; set; }

    /// <summary>Note text content.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Date of the interaction or note.</summary>
    public DateTime NoteDate { get; set; }

    /// <summary>Type of note — references DefinedValue (DefinedType: "Note Type").</summary>
    public int? NoteTypeDefinedValueId { get; set; }

    /// <summary>Whether this note is private and should only be visible to staff.</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Whether this note is an alert that should be prominently displayed.</summary>
    public bool IsAlert { get; set; }

    // Navigation properties
    public virtual Person Person { get; set; } = null!;
    public virtual DefinedValue? NoteTypeDefinedValue { get; set; }
    public virtual PersonAlias? CreatedByPersonAlias { get; set; }
}
