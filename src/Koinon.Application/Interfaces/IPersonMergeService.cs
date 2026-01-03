using Koinon.Application.Common;
using Koinon.Application.DTOs.PersonMerge;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for merging duplicate person records.
/// </summary>
public interface IPersonMergeService
{
    /// <summary>
    /// Gets a side-by-side comparison of two persons to aid in merge decision.
    /// </summary>
    /// <param name="person1IdKey">IdKey of the first person.</param>
    /// <param name="person2IdKey">IdKey of the second person.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Comparison details including record counts.</returns>
    Task<PersonComparisonDto?> ComparePersonsAsync(
        string person1IdKey,
        string person2IdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Merges two person records, updating all foreign key references.
    /// </summary>
    /// <param name="request">Merge request with survivor/merged selection and field preferences.</param>
    /// <param name="currentUserId">ID of the user performing the merge.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing counts of updated records or error.</returns>
    Task<Result<PersonMergeResultDto>> MergeAsync(
        PersonMergeRequestDto request,
        int currentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the history of person merge operations.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of merge history records.</returns>
    Task<PagedResult<PersonMergeHistoryDto>> GetMergeHistoryAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);
}
