namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO representing a batch summary with reconciliation status.
/// </summary>
public record BatchSummaryDto
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
    /// Actual total amount from contributions.
    /// </summary>
    public required decimal ActualAmount { get; init; }

    /// <summary>
    /// Number of contributions in the batch.
    /// </summary>
    public required int ContributionCount { get; init; }

    /// <summary>
    /// Difference between control item count and actual contribution count.
    /// </summary>
    public int? ItemCountVariance { get; init; }

    /// <summary>
    /// Difference between control amount and actual amount.
    /// </summary>
    public required decimal Variance { get; init; }

    /// <summary>
    /// Whether the batch is balanced (variance is zero).
    /// </summary>
    public required bool IsBalanced { get; init; }
}
