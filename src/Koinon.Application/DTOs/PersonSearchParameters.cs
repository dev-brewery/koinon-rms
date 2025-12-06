namespace Koinon.Application.DTOs;

/// <summary>
/// Parameters for searching people.
/// </summary>
public record PersonSearchParameters
{
    /// <summary>
    /// Full-text search query (searches first name, last name, nick name, email).
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Filter by primary campus ID.
    /// </summary>
    public string? CampusId { get; init; }

    /// <summary>
    /// Filter by record status ID.
    /// </summary>
    public string? RecordStatusId { get; init; }

    /// <summary>
    /// Filter by connection status ID.
    /// </summary>
    public string? ConnectionStatusId { get; init; }

    /// <summary>
    /// Include inactive records (default: false).
    /// </summary>
    public bool IncludeInactive { get; init; }

    /// <summary>
    /// Page number (1-based, default: 1).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page (default: 25, max: 100).
    /// </summary>
    public int PageSize { get; init; } = 25;
}
