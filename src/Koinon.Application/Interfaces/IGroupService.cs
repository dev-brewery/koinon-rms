using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for group management operations (non-family groups).
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Gets a group by their integer ID with all members.
    /// </summary>
    Task<GroupDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets a group by their IdKey with all members.
    /// </summary>
    Task<GroupDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Searches for groups with pagination and filtering.
    /// </summary>
    Task<PagedResult<GroupSummaryDto>> SearchAsync(
        GroupSearchParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new group.
    /// </summary>
    Task<Result<GroupDto>> CreateAsync(
        CreateGroupRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing group.
    /// </summary>
    Task<Result<GroupDto>> UpdateAsync(
        string idKey,
        UpdateGroupRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a group (archives it).
    /// </summary>
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Adds a person as a group member with a specific role.
    /// </summary>
    Task<Result<GroupMemberDto>> AddMemberAsync(
        string groupIdKey,
        AddGroupMemberRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a person from a group.
    /// </summary>
    Task<Result> RemoveMemberAsync(
        string groupIdKey,
        string personIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all members of a group.
    /// </summary>
    Task<IReadOnlyList<GroupMemberDto>> GetMembersAsync(
        string groupIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the parent group if this group is part of a hierarchy.
    /// </summary>
    Task<GroupSummaryDto?> GetParentGroupAsync(
        string groupIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all child groups (subgroups) of a group.
    /// </summary>
    Task<IReadOnlyList<GroupSummaryDto>> GetChildGroupsAsync(
        string groupIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all schedules associated with a group.
    /// </summary>
    Task<IReadOnlyList<GroupScheduleDto>> GetSchedulesAsync(
        string groupIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a schedule to a group.
    /// </summary>
    Task<Result<GroupScheduleDto>> AddScheduleAsync(
        string groupIdKey,
        AddGroupScheduleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a schedule from a group.
    /// </summary>
    Task<Result> RemoveScheduleAsync(
        string groupIdKey,
        string scheduleIdKey,
        CancellationToken ct = default);
}
