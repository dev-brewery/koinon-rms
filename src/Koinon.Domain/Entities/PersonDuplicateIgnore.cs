namespace Koinon.Domain.Entities;

/// <summary>
/// Tracks pairs of Person records that have been manually marked as "not duplicates"
/// to prevent duplicate detection algorithms from repeatedly flagging them.
/// The pair is stored with PersonId1 &lt; PersonId2 to ensure uniqueness.
/// </summary>
public class PersonDuplicateIgnore : Entity
{
    /// <summary>
    /// Foreign key to the first Person in the pair (always the smaller ID).
    /// </summary>
    public required int PersonId1 { get; set; }

    /// <summary>
    /// Foreign key to the second Person in the pair (always the larger ID).
    /// </summary>
    public required int PersonId2 { get; set; }

    /// <summary>
    /// Foreign key to the Person who marked this pair as not duplicates.
    /// Nullable to handle system-initiated actions or when the user is unknown.
    /// </summary>
    public int? MarkedByPersonId { get; set; }

    /// <summary>
    /// Date and time when the pair was marked as not duplicates.
    /// </summary>
    public required DateTime MarkedDateTime { get; set; }

    /// <summary>
    /// Optional explanation of why these records are not duplicates
    /// (e.g., "Father and son with same name", "Different people with similar info").
    /// </summary>
    public string? Reason { get; set; }

    // Navigation Properties

    /// <summary>
    /// The first Person in the ignored pair.
    /// </summary>
    public virtual Person? Person1 { get; set; }

    /// <summary>
    /// The second Person in the ignored pair.
    /// </summary>
    public virtual Person? Person2 { get; set; }

    /// <summary>
    /// The Person who marked this pair as not duplicates.
    /// </summary>
    public virtual Person? MarkedByPerson { get; set; }
}
