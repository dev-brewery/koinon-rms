using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a batch of contributions grouped for processing.
/// Batches organize contributions for reconciliation and posting.
/// </summary>
public class ContributionBatch : Entity
{
    /// <summary>
    /// Name of the batch.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Date when the batch was created or intended for posting.
    /// </summary>
    public required DateTime BatchDate { get; set; }

    /// <summary>
    /// Current status of the batch (Open, Closed, Posted).
    /// </summary>
    public BatchStatus Status { get; set; } = BatchStatus.Open;

    /// <summary>
    /// Expected total amount for reconciliation purposes.
    /// Nullable - not all batches have a predetermined control total.
    /// </summary>
    public decimal? ControlAmount { get; set; }

    /// <summary>
    /// Optional campus association for the batch.
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// Optional notes about the batch.
    /// </summary>
    public string? Note { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated campus.
    /// </summary>
    public virtual Campus? Campus { get; set; }

    /// <summary>
    /// Contributions included in this batch.
    /// </summary>
    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
}
