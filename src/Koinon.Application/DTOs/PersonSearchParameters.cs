namespace Koinon.Application.DTOs;

/// <summary>
/// Parameters for searching people.
/// </summary>
public class PersonSearchParameters
{
    /// <summary>
    /// Full-text search query (searches first name, last name, nick name, email).
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by primary campus ID.
    /// </summary>
    public string? CampusId { get; set; }

    /// <summary>
    /// Filter by record status ID.
    /// </summary>
    public string? RecordStatusId { get; set; }

    /// <summary>
    /// Filter by connection status ID.
    /// </summary>
    public string? ConnectionStatusId { get; set; }

    /// <summary>
    /// Include inactive records (default: false).
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
