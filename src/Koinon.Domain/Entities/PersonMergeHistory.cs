namespace Koinon.Domain.Entities;

/// <summary>
/// Tracks person merge operations for audit and potential recovery.
/// Records which person records were merged together and who performed the operation.
/// </summary>
public class PersonMergeHistory : Entity
{
    /// <summary>
    /// Foreign key to the Person who survived the merge (remains active).
    /// </summary>
    public required int SurvivorPersonId { get; set; }

    /// <summary>
    /// Foreign key to the Person who was merged into the survivor (becomes inactive).
    /// </summary>
    public required int MergedPersonId { get; set; }

    /// <summary>
    /// Foreign key to the Person who performed the merge operation.
    /// Nullable to handle system-initiated merges or when the user is unknown.
    /// </summary>
    public int? MergedByPersonId { get; set; }

    /// <summary>
    /// Date and time when the merge operation was performed.
    /// </summary>
    public required DateTime MergedDateTime { get; set; }

    /// <summary>
    /// Optional notes explaining the reason for the merge or any special considerations.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties

    /// <summary>
    /// The Person who survived the merge and retained all merged data.
    /// </summary>
    public virtual Person? SurvivorPerson { get; set; }

    /// <summary>
    /// The Person whose record was merged and marked inactive.
    /// </summary>
    public virtual Person? MergedPerson { get; set; }

    /// <summary>
    /// The Person who performed the merge operation.
    /// </summary>
    public virtual Person? MergedByPerson { get; set; }
}
