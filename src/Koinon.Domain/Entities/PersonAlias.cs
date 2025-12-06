namespace Koinon.Domain.Entities;

/// <summary>
/// Represents an alias or alternate reference to a Person entity.
/// Used for tracking merged persons and maintaining referential integrity across person merges.
/// </summary>
/// <remarks>
/// PersonAlias allows all foreign keys throughout the system to reference PersonAlias instead of Person directly.
/// When Person A is merged into Person B, Person A's PersonAlias records are updated to point to Person B,
/// preserving all historical relationships.
///
/// Design Note: PersonId is nullable to support merge-only records where we only need to track
/// the merge history (via AliasPersonId/AliasPersonGuid) without a current person reference.
/// At least one of PersonId or AliasPersonId should be set - this is validated at the application layer.
/// </remarks>
public class PersonAlias : Entity
{
    /// <summary>
    /// Foreign key to the Person this alias currently represents.
    /// Nullable to support merge-only records that track history without a current person reference.
    /// </summary>
    public int? PersonId { get; set; }

    /// <summary>
    /// Optional alternate name for this alias if different from the person's current name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Foreign key to another Person if this alias was created during a person merge.
    /// Points to the "merged from" person.
    /// </summary>
    public int? AliasPersonId { get; set; }

    /// <summary>
    /// GUID of the person this alias was merged from (for reference tracking).
    /// </summary>
    public Guid? AliasPersonGuid { get; set; }

    // Navigation properties
    /// <summary>
    /// The Person entity that this alias currently points to.
    /// </summary>
    public virtual Person? Person { get; set; }
}
