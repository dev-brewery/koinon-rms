namespace Koinon.Application.DTOs;

/// <summary>
/// Parameters for searching families.
/// </summary>
public class FamilySearchParameters
{
    /// <summary>
    /// Full-text search query (searches family name or member names).
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by campus ID.
    /// </summary>
    public string? CampusId { get; set; }

    /// <summary>
    /// Include inactive families (default: false).
    /// </summary>
    public bool IncludeInactive { get; set; }

    /// <summary>
    /// Page number (1-based, default: 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page (default: 25, max: 100).
    /// </summary>
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Ensure PageSize is within valid range.
    /// </summary>
    public void ValidatePageSize()
    {
        if (PageSize < 1)
        {
            PageSize = 25;
        }

        if (PageSize > 100)
        {
            PageSize = 100;
        }
    }
}
