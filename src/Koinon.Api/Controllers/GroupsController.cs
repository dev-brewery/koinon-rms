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
        // Validate optional IdKey parameters
        if (!string.IsNullOrWhiteSpace(groupTypeId) && !IdKeyValidator.IsValid(groupTypeId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("groupTypeId"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (!string.IsNullOrWhiteSpace(campusId) && !IdKeyValidator.IsValid(campusId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("campusId"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (!string.IsNullOrWhiteSpace(parentGroupId) && !IdKeyValidator.IsValid(parentGroupId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("parentGroupId"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

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

        return Ok(result);
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
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var group = await groupService.GetByIdKeyAsync(idKey, ct);

        if (group == null)
        {
            logger.LogWarning("Group not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Group not found",
                Detail = $"No group found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Group retrieved: IdKey={IdKey}, Name={Name}", idKey, group.Name);

        return Ok(group);
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
            group);
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
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

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

        return Ok(group);
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
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

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

        logger.LogInformation("Group archived successfully: IdKey={IdKey}", idKey);

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
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var members = await groupService.GetMembersAsync(idKey, ct);

        logger.LogInformation(
            "Group members retrieved: IdKey={IdKey}, MemberCount={MemberCount}",
            idKey, members.Count);

        return Ok(members);
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
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

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
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (!IdKeyValidator.IsValid(personIdKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("personIdKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

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
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var childGroups = await groupService.GetChildGroupsAsync(idKey, ct);

        logger.LogInformation(
            "Child groups retrieved: IdKey={IdKey}, ChildCount={ChildCount}",
            idKey, childGroups.Count);

        return Ok(childGroups);
    }
}
