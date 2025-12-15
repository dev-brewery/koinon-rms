using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Public API endpoints for group discovery.
/// These endpoints do not require authentication and expose only safe, public-facing group information.
/// </summary>
[ApiController]
[Route("api/v1/groups/public")]
[AllowAnonymous]
public class PublicGroupsController(
    IPublicGroupService publicGroupService,
    ILogger<PublicGroupsController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for publicly visible groups with optional filters.
    /// </summary>
    /// <param name="searchTerm">Text search across group name and description</param>
    /// <param name="groupTypeIdKey">Filter by group type IdKey</param>
    /// <param name="campusIdKey">Filter by campus IdKey</param>
    /// <param name="dayOfWeek">Filter by meeting day of week (0=Sunday, 6=Saturday)</param>
    /// <param name="timeOfDay">Filter by meeting time range (0=Morning, 1=Afternoon, 2=Evening)</param>
    /// <param name="hasOpenings">Filter to groups with available capacity</param>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of public groups</returns>
    /// <response code="200">Returns paginated list of public groups</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PublicGroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchGroups(
        [FromQuery] string? searchTerm,
        [FromQuery] string? groupTypeIdKey,
        [FromQuery] string? campusIdKey,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] TimeRange? timeOfDay,
        [FromQuery] bool? hasOpenings,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Validate enum parameters
        if (timeOfDay.HasValue && !Enum.IsDefined(typeof(TimeRange), timeOfDay.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid parameter",
                Detail = $"'{timeOfDay}' is not a valid time of day. Valid values: Morning (0), Afternoon (1), Evening (2)",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (dayOfWeek.HasValue && !Enum.IsDefined(typeof(DayOfWeek), dayOfWeek.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid parameter",
                Detail = $"'{dayOfWeek}' is not a valid day of week. Valid values: Sunday (0) through Saturday (6)",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var parameters = new PublicGroupSearchParameters
        {
            SearchTerm = searchTerm,
            GroupTypeIdKey = groupTypeIdKey,
            CampusIdKey = campusIdKey,
            DayOfWeek = dayOfWeek,
            TimeOfDay = timeOfDay,
            HasOpenings = hasOpenings,
            PageNumber = pageNumber,
            PageSize = Math.Min(pageSize, 100) // Enforce max page size
        };

        logger.LogInformation(
            "Public group search: Term={SearchTerm}, Type={Type}, Campus={Campus}, Day={Day}, Time={Time}, HasOpenings={HasOpenings}",
            searchTerm, groupTypeIdKey, campusIdKey, dayOfWeek, timeOfDay, hasOpenings);

        var result = await publicGroupService.SearchPublicGroupsAsync(parameters, ct);

        logger.LogDebug(
            "Public group search completed: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            result.Page, result.PageSize, result.TotalCount);

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
    /// Gets public details for a specific group.
    /// Only returns data if the group exists and is marked as public.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Public group details</returns>
    /// <response code="200">Returns group public details</response>
    /// <response code="404">Group not found or not public</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(PublicGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(
        string idKey,
        CancellationToken ct = default)
    {
        logger.LogDebug("Retrieving public group: IdKey={IdKey}", idKey);

        var group = await publicGroupService.GetPublicGroupAsync(idKey, ct);

        if (group is null)
        {
            logger.LogDebug("Public group not found or not public: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Group not found",
                Detail = $"No public group found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Public group retrieved: IdKey={IdKey}, Name={Name}", idKey, group.Name);

        return Ok(new { data = group });
    }
}
