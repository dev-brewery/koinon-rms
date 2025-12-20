namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to filter and paginate contribution batches.
/// </summary>
public record BatchFilterRequest
{
    /// <summary>
    /// Filter by batch status (Open, Closed, Posted).
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Filter by campus IdKey.
    /// </summary>
    public string? CampusIdKey { get; init; }

    /// <summary>
    /// Filter batches created on or after this date.
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Filter batches created on or before this date.
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 50;
}
