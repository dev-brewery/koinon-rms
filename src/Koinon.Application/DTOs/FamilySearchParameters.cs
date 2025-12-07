namespace Koinon.Application.DTOs;

/// <summary>
/// Parameters for searching families.
/// </summary>
public record FamilySearchParameters
{
    /// <summary>
    /// Full-text search query (searches family name or member names).
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Filter by campus ID.
    /// </summary>
    public string? CampusId { get; init; }

    /// <summary>
    /// Include inactive families (default: false).
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
