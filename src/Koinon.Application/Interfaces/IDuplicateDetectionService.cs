using Koinon.Application.Common;
using Koinon.Application.DTOs.PersonMerge;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for detecting potential duplicate person records.
/// </summary>
public interface IDuplicateDetectionService
{
    /// <summary>
    /// Finds all potential duplicate person pairs in the system.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of potential duplicate matches.</returns>
    Task<PagedResult<DuplicateMatchDto>> FindDuplicatesAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Finds potential duplicates for a specific person.
    /// </summary>
    /// <param name="personIdKey">IdKey of the person to find duplicates for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of potential duplicate matches for the specified person.</returns>
    Task<List<DuplicateMatchDto>> FindDuplicatesForPersonAsync(
        string personIdKey,
        CancellationToken ct = default);
}
