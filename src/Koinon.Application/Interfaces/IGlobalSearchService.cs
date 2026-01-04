using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for performing global searches across multiple entity types.
/// </summary>
public interface IGlobalSearchService
{
    /// <summary>
    /// Searches across People, Families, and Groups based on the provided query.
    /// </summary>
    /// <param name="query">Search term to match against entity fields.</param>
    /// <param name="category">Optional filter for specific category ("People", "Families", or "Groups").</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of results per page (max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated search results with category counts.</returns>
    Task<GlobalSearchResponse> SearchAsync(
        string query,
        string? category = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
