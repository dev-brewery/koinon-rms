using Koinon.Application.Common;
using Koinon.Application.DTOs.PersonMerge;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing ignored duplicate person pairs.
/// </summary>
public interface IDuplicateIgnoreService
{
    /// <summary>
    /// Marks a pair of persons as "not duplicates" to exclude them from duplicate detection.
    /// </summary>
    /// <param name="request">Request containing the two person IdKeys and optional reason.</param>
    /// <param name="currentUserId">ID of the user marking the pair.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or error result.</returns>
    Task<Result> IgnoreDuplicateAsync(
        IgnoreDuplicateRequestDto request,
        int currentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Removes the "ignore" flag from a duplicate pair, allowing them to appear in detection again.
    /// </summary>
    /// <param name="person1IdKey">IdKey of the first person.</param>
    /// <param name="person2IdKey">IdKey of the second person.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or error result.</returns>
    Task<Result> UnignoreDuplicateAsync(
        string person1IdKey,
        string person2IdKey,
        CancellationToken ct = default);
}
