using Koinon.Api.Filters;
using Koinon.Api.Helpers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for group management operations.
/// Provides endpoints for searching, creating, updating, and managing groups and their members.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ValidateIdKey]
public class GroupsController(
    IGroupService groupService,
    ILogger<GroupsController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for groups with optional filters and pagination.
    /// </summary>
    /// <param name="query">Full-text search query</param>
    /// <param name="groupTypeId">Filter by group type IdKey</param>
    /// <param name="campusId">Filter by campus IdKey</param>
    /// <param name="parentGroupId">Filter by parent group IdKey</param>
    /// <param name="includeInactive">Include inactive groups</param>
    /// <param name="includeArchived">Include archived groups</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of groups</returns>
    /// <response code="200">Returns paginated list of groups</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GroupSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] string? groupTypeId,
        [FromQuery] string? campusId,
        [FromQuery] string? parentGroupId,
        [FromQuery] bool includeInactive = false,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var parameters = new GroupSearchParameters
        {
            Query = query,
            GroupTypeId = groupTypeId,
            CampusId = campusId,
            ParentGroupId = parentGroupId,
            IncludeInactive = includeInactive,
            IncludeArchived = includeArchived,
            Page = page,
            PageSize = pageSize
        };

        var result = await groupService.SearchAsync(parameters, ct);

        logger.LogInformation(
            "Group search completed: Query={Query}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            query, result.Page, result.PageSize, result.TotalCount);

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
            }
        });
    }

    /// <summary>
    /// Gets a group by its IdKey with full details including members and child groups.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Group details</returns>
    /// <response code="200">Returns group details</response>
    /// <response code="404">Group not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var group = await groupService.GetByIdKeyAsync(idKey, ct);

        if (group == null)
        {
            logger.LogDebug("Group not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Group not found",
                Detail = $"No group found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Group retrieved: IdKey={IdKey}, Name={Name}", idKey, group.Name);

        return Ok(new { data = group });
    }

    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <param name="request">Group creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created group details</returns>
    /// <response code="201">Group created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request, CancellationToken ct = default)
    {
        var result = await groupService.CreateAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create group: Code={Code}, Message={Message}",
                result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Message,
                    Detail = result.Error.Details != null
                        ? string.Join("; ", result.Error.Details.SelectMany(kvp => kvp.Value))
                        : null,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details }
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        var group = result.Value!;

        logger.LogInformation(
            "Group created successfully: IdKey={IdKey}, Name={Name}",
            group.IdKey, group.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = group.IdKey },
            new { data = group });
    }

    /// <summary>
    /// Updates an existing group.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="request">Group update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated group details</returns>
    /// <response code="200">Group updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Group not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateGroupRequest request,
        CancellationToken ct = default)
    {
        var result = await groupService.UpdateAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update group: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Group not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Message,
                    Detail = result.Error.Details != null
                        ? string.Join("; ", result.Error.Details.SelectMany(kvp => kvp.Value))
                        : null,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details }
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        var group = result.Value!;

        logger.LogInformation(
            "Group updated successfully: IdKey={IdKey}, Name={Name}",
            group.IdKey, group.Name);

        return Ok(new { data = group });
    }

    /// <summary>
    /// Soft-deletes a group (archives it).
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Group archived successfully</response>
    /// <response code="404">Group not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await groupService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to archive group: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Group not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogDebug("Group archived successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Gets all members of a group.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of group members</returns>
    /// <response code="200">Returns list of group members</response>
    /// <response code="400">Invalid IdKey format</response>
    [HttpGet("{idKey}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<GroupMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMembers(string idKey, CancellationToken ct = default)
    {
        var members = await groupService.GetMembersAsync(idKey, ct);

        logger.LogInformation(
            "Group members retrieved: IdKey={IdKey}, MemberCount={MemberCount}",
            idKey, members.Count);

        return Ok(new { data = members });
    }

    /// <summary>
    /// Adds a person as a group member with a specific role.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="request">Member addition details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created group member details</returns>
    /// <response code="201">Member added successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Group or person not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost("{idKey}/members")]
    [ProducesResponseType(typeof(GroupMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddMember(
        string idKey,
        [FromBody] AddGroupMemberRequest request,
        CancellationToken ct = default)
    {
        var result = await groupService.AddMemberAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to add member to group: GroupIdKey={GroupIdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Message,
                    Detail = result.Error.Details != null
                        ? string.Join("; ", result.Error.Details.SelectMany(kvp => kvp.Value))
                        : null,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details }
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        var member = result.Value!;

        logger.LogInformation(
            "Member added to group successfully: GroupIdKey={GroupIdKey}, PersonIdKey={PersonIdKey}",
            idKey, member.Person.IdKey);

        return CreatedAtAction(
            nameof(GetMembers),
            new { idKey },
            member);
    }

    /// <summary>
    /// Removes a person from a group.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="personIdKey">The person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Member removed successfully</response>
    /// <response code="400">Invalid IdKey format</response>
    /// <response code="404">Group or member not found</response>
    [HttpDelete("{idKey}/members/{personIdKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        string idKey,
        string personIdKey,
        CancellationToken ct = default)
    {
        var result = await groupService.RemoveMemberAsync(idKey, personIdKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to remove member from group: GroupIdKey={GroupIdKey}, PersonIdKey={PersonIdKey}, Code={Code}, Message={Message}",
                idKey, personIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogInformation(
            "Member removed from group successfully: GroupIdKey={GroupIdKey}, PersonIdKey={PersonIdKey}",
            idKey, personIdKey);

        return NoContent();
    }

    /// <summary>
    /// Gets all child groups (subgroups) of a group.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of child groups</returns>
    /// <response code="200">Returns list of child groups</response>
    /// <response code="400">Invalid IdKey format</response>
    [HttpGet("{idKey}/children")]
    [ProducesResponseType(typeof(IReadOnlyList<GroupSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetChildren(string idKey, CancellationToken ct = default)
    {
        var childGroups = await groupService.GetChildGroupsAsync(idKey, ct);

        logger.LogInformation(
            "Child groups retrieved: IdKey={IdKey}, ChildCount={ChildCount}",
            idKey, childGroups.Count);

        return Ok(new { data = childGroups });
    }

    /// <summary>
    /// Gets all schedules associated with a group.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of group schedules</returns>
    /// <response code="200">Returns list of schedules</response>
    /// <response code="400">Invalid IdKey format</response>
    [HttpGet("{idKey}/schedules")]
    [ProducesResponseType(typeof(IReadOnlyList<GroupScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSchedules(string idKey, CancellationToken ct = default)
    {
        var schedules = await groupService.GetSchedulesAsync(idKey, ct);

        logger.LogInformation(
            "Group schedules retrieved: IdKey={IdKey}, ScheduleCount={ScheduleCount}",
            idKey, schedules.Count);

        return Ok(new { data = schedules });
    }

    /// <summary>
    /// Adds a schedule to a group.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="request">Schedule addition details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created group schedule</returns>
    /// <response code="201">Schedule added successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Group or schedule not found</response>
    /// <response code="422">Business rule violation (duplicate)</response>
    [HttpPost("{idKey}/schedules")]
    [ProducesResponseType(typeof(GroupScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddSchedule(
        string idKey,
        [FromBody] AddGroupScheduleRequest request,
        CancellationToken ct = default)
    {
        var result = await groupService.AddScheduleAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to add schedule to group: GroupIdKey={GroupIdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "DUPLICATE" => UnprocessableEntity(new ProblemDetails
                {
                    Title = "Duplicate association",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        var groupSchedule = result.Value!;

        logger.LogInformation(
            "Schedule added to group successfully: GroupIdKey={GroupIdKey}, ScheduleIdKey={ScheduleIdKey}",
            idKey, groupSchedule.Schedule.IdKey);

        return CreatedAtAction(
            nameof(GetSchedules),
            new { idKey },
            groupSchedule);
    }

    /// <summary>
    /// Removes a schedule from a group.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="scheduleIdKey">The schedule's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Schedule removed successfully</response>
    /// <response code="400">Invalid IdKey format</response>
    /// <response code="404">Group or schedule association not found</response>
    [HttpDelete("{idKey}/schedules/{scheduleIdKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSchedule(
        string idKey,
        string scheduleIdKey,
        CancellationToken ct = default)
    {
        var result = await groupService.RemoveScheduleAsync(idKey, scheduleIdKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to remove schedule from group: GroupIdKey={GroupIdKey}, ScheduleIdKey={ScheduleIdKey}, Code={Code}, Message={Message}",
                idKey, scheduleIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogInformation(
            "Schedule removed from group successfully: GroupIdKey={GroupIdKey}, ScheduleIdKey={ScheduleIdKey}",
            idKey, scheduleIdKey);

        return NoContent();
    }
}
