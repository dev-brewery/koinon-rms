using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for managing groups where the current user is a leader.
/// </summary>
public interface IMyGroupsService
{
    /// <summary>
    /// Gets all groups where the current user is a leader.
    /// </summary>
    Task<IReadOnlyList<MyGroupDto>> GetMyGroupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets detailed member information for a group (only if current user is a leader).
    /// Includes contact information that is not available in public views.
    /// </summary>
    Task<Result<IReadOnlyList<GroupMemberDetailDto>>> GetGroupMembersWithContactInfoAsync(
        string groupIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a group member's role or status (only if current user is a leader).
    /// </summary>
    Task<Result<GroupMemberDetailDto>> UpdateGroupMemberAsync(
        string groupIdKey,
        string memberIdKey,
        UpdateGroupMemberRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a member from the group (only if current user is a leader).
    /// </summary>
    Task<Result> RemoveGroupMemberAsync(
        string groupIdKey,
        string memberIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Records attendance for a group meeting (only if current user is a leader).
    /// </summary>
    Task<Result> RecordAttendanceAsync(
        string groupIdKey,
        RecordAttendanceRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether the current user is a leader of the specified group, or a staff/admin user.
    /// Used by other controllers to enforce leader-or-staff authorization on group-scoped endpoints.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the current user is a leader of the group or has staff/admin role.</returns>
    Task<bool> IsGroupLeaderOrStaffAsync(string groupIdKey, CancellationToken ct = default);
}
