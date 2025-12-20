namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO representing a contribution batch.
/// </summary>
public record ContributionBatchDto
{
    /// <summary>
    /// URL-safe IdKey for the batch.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Name of the batch.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Date when the batch was created or intended for posting.
    /// </summary>
    public required DateTime BatchDate { get; init; }

    /// <summary>
    /// Current status of the batch (Open, Closed, Posted).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Expected total amount for reconciliation purposes.
    /// </summary>
    public decimal? ControlAmount { get; init; }

    /// <summary>
    /// Expected number of items for reconciliation purposes.
    /// </summary>
    public int? ControlItemCount { get; init; }

    /// <summary>
    /// Associated campus IdKey.
    /// </summary>
    public string? CampusIdKey { get; init; }

    /// <summary>
    /// Optional notes about the batch.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// When the batch was created.
    /// </summary>
    public required DateTime CreatedDateTime { get; init; }

    /// <summary>
    /// When the batch was last modified.
    /// </summary>
    public DateTime? ModifiedDateTime { get; init; }
}
