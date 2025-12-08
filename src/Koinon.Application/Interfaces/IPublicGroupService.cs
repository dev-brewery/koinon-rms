using Koinon.Application.Common;
using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for public group search and discovery.
/// Provides read-only access to publicly visible group information.
/// </summary>
public interface IPublicGroupService
{
    /// <summary>
    /// Searches for publicly visible groups based on the provided parameters.
    /// Only returns groups where IsPublic = true, IsActive = true, and IsArchived = false.
    /// </summary>
    /// <param name="parameters">Search and filter parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of public groups.</returns>
    Task<PagedResult<PublicGroupDto>> SearchPublicGroupsAsync(
        PublicGroupSearchParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a single public group by its IdKey.
    /// Returns null if the group does not exist or is not public.
    /// </summary>
    /// <param name="groupIdKey">The IdKey of the group.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The public group DTO or null if not found.</returns>
    Task<PublicGroupDto?> GetPublicGroupAsync(
        string groupIdKey,
        CancellationToken ct = default);
}
