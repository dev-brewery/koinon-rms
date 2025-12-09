using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing group membership requests.
/// </summary>
public interface IGroupMemberRequestService
{
    /// <summary>
    /// Submits a membership request for the authenticated user to join a group.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey.</param>
    /// <param name="request">Request details including optional note.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created membership request.</returns>
    Task<Result<GroupMemberRequestDto>> SubmitRequestAsync(
        string groupIdKey,
        SubmitMembershipRequestDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all pending membership requests for a group.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending membership requests.</returns>
    Task<Result<IReadOnlyList<GroupMemberRequestDto>>> GetPendingRequestsAsync(
        string groupIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Processes (approves or denies) a membership request.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey.</param>
    /// <param name="requestIdKey">The request's IdKey.</param>
    /// <param name="request">Processing details including status and optional note.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated membership request.</returns>
    Task<Result<GroupMemberRequestDto>> ProcessRequestAsync(
        string groupIdKey,
        string requestIdKey,
        ProcessMembershipRequestDto request,
        CancellationToken ct = default);
}
