namespace Koinon.Application.DTOs;

/// <summary>
/// Represents a single search result from global search.
/// </summary>
public record GlobalSearchResultDto(
    string Category,    // "People", "Families", "Groups"
    string IdKey,       // URL-safe Base64 encoded ID
    string Title,       // Primary display (e.g., full name, family name, group name)
    string? Subtitle,   // Secondary info (e.g., email, location)
    string? ImageUrl    // Optional profile photo URL
);

/// <summary>
/// Response containing paginated global search results with category counts.
/// </summary>
public record GlobalSearchResponse(
    IReadOnlyList<GlobalSearchResultDto> Results,
    int TotalCount,
    int PageNumber,
    int PageSize,
    Dictionary<string, int> CategoryCounts  // {"People": 15, "Families": 5, "Groups": 3}
);
