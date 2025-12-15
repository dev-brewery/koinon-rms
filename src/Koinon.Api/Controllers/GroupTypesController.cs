using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Admin endpoints for managing group types.
/// Provides configuration and management of different types of groups in the system.
/// </summary>
[ApiController]
[Route("api/v1/admin/group-types")]
[Authorize(Roles = "Admin")]
[ValidateIdKey]
public class GroupTypesController(
    IGroupTypeService groupTypeService,
    ILogger<GroupTypesController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all group types in the system.
    /// </summary>
    /// <param name="includeArchived">Include archived group types (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all group types</returns>
    /// <response code="200">Returns list of group types</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default)
    {
        var groupTypes = await groupTypeService.GetAllGroupTypesAsync(includeArchived, ct);

        logger.LogInformation(
            "Retrieved {Count} group types (includeArchived: {IncludeArchived})",
            groupTypes.Count,
            includeArchived);

        return Ok(new { data = groupTypes });
    }

    /// <summary>
    /// Gets a specific group type by IdKey.
    /// </summary>
    /// <param name="idKey">The group type's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Group type details</returns>
    /// <response code="200">Returns group type details</response>
    /// <response code="404">Group type not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(GroupTypeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var groupType = await groupTypeService.GetGroupTypeByIdKeyAsync(idKey, ct);

        if (groupType == null)
        {
            logger.LogDebug("Group type not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Group type not found",
                Detail = $"No group type found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Group type retrieved: IdKey={IdKey}, Name={Name}", idKey, groupType.Name);

        return Ok(new { data = groupType });
    }

    /// <summary>
    /// Creates a new group type.
    /// </summary>
    /// <param name="request">Group type creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created group type details</returns>
    /// <response code="201">Group type created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(GroupTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateGroupTypeRequest request, CancellationToken ct = default)
    {
        var result = await groupTypeService.CreateGroupTypeAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create group type: Code={Code}, Message={Message}",
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

        var groupType = result.Value!;

        logger.LogInformation(
            "Group type created successfully: IdKey={IdKey}, Name={Name}",
            groupType.IdKey, groupType.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = groupType.IdKey },
            groupType);
    }

    /// <summary>
    /// Updates an existing group type.
    /// </summary>
    /// <param name="idKey">The group type's IdKey</param>
    /// <param name="request">Group type update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated group type details</returns>
    /// <response code="200">Group type updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Group type not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(GroupTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateGroupTypeRequest request,
        CancellationToken ct = default)
    {
        var result = await groupTypeService.UpdateGroupTypeAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update group type: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Group type not found",
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

        var groupType = result.Value!;

        logger.LogInformation(
            "Group type updated successfully: IdKey={IdKey}, Name={Name}",
            groupType.IdKey, groupType.Name);

        return Ok(new { data = groupType });
    }

    /// <summary>
    /// Archives a group type (soft delete).
    /// </summary>
    /// <param name="idKey">The group type's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Group type archived successfully</response>
    /// <response code="404">Group type not found</response>
    /// <response code="422">Business rule violation (cannot archive if in use)</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Archive(string idKey, CancellationToken ct = default)
    {
        var result = await groupTypeService.ArchiveGroupTypeAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to archive group type: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Group type not found",
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

        logger.LogInformation("Group type archived successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Gets all groups of a specific type.
    /// </summary>
    /// <param name="idKey">The group type's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of groups with this type</returns>
    /// <response code="200">Returns list of groups</response>
    /// <response code="404">Group type not found</response>
    [HttpGet("{idKey}/groups")]
    [ProducesResponseType(typeof(IReadOnlyList<GroupSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroups(string idKey, CancellationToken ct = default)
    {
        // First verify the group type exists
        var groupType = await groupTypeService.GetGroupTypeByIdKeyAsync(idKey, ct);
        if (groupType == null)
        {
            logger.LogDebug("Group type not found when fetching groups: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Group type not found",
                Detail = $"No group type found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var groups = await groupTypeService.GetGroupsByTypeAsync(idKey, ct);

        logger.LogInformation(
            "Groups retrieved for group type: IdKey={IdKey}, GroupCount={GroupCount}",
            idKey, groups.Count);

        return Ok(new { data = groups });
    }
}
