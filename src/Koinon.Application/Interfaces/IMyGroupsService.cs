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
}
