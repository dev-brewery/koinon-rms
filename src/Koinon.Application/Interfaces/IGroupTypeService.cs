using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing group types.
/// </summary>
public interface IGroupTypeService
{
    /// <summary>
    /// Gets all group types.
    /// </summary>
    /// <param name="includeArchived">Whether to include archived group types.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of group types.</returns>
    Task<IReadOnlyList<GroupTypeDto>> GetAllGroupTypesAsync(bool includeArchived = false, CancellationToken ct = default);

    /// <summary>
    /// Gets a group type by its IdKey.
    /// </summary>
    /// <param name="idKey">The IdKey of the group type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The group type details or null if not found.</returns>
    Task<GroupTypeDetailDto?> GetGroupTypeByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Creates a new group type.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created group type or an error.</returns>
    Task<Result<GroupTypeDto>> CreateGroupTypeAsync(CreateGroupTypeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing group type.
    /// </summary>
    /// <param name="idKey">The IdKey of the group type to update.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated group type or an error.</returns>
    Task<Result<GroupTypeDto>> UpdateGroupTypeAsync(string idKey, UpdateGroupTypeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Archives a group type.
    /// </summary>
    /// <param name="idKey">The IdKey of the group type to archive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if archived successfully, or an error.</returns>
    Task<Result<bool>> ArchiveGroupTypeAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Gets all groups of a specific type.
    /// </summary>
    /// <param name="groupTypeIdKey">The IdKey of the group type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of groups of the specified type.</returns>
    Task<IReadOnlyList<GroupSummaryDto>> GetGroupsByTypeAsync(string groupTypeIdKey, CancellationToken ct = default);
}
