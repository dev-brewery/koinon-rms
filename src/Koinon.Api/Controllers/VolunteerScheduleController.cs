using Koinon.Api.Filters;
using Koinon.Application.DTOs.VolunteerSchedule;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for managing volunteer schedule assignments.
/// Handles assigning volunteers to serving schedules and tracking confirmations.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[ValidateIdKey]
public class VolunteerScheduleController(
    IVolunteerScheduleService volunteerScheduleService,
    IUserContext userContext,
    ILogger<VolunteerScheduleController> logger) : ControllerBase
{
    /// <summary>
    /// Creates volunteer schedule assignments for a group.
    /// Assigns multiple volunteers to specific dates on a schedule.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="request">Assignment creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of created assignments</returns>
    /// <response code="201">Assignments created successfully</response>
    /// <response code="400">Validation failed or double-booking detected</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires GroupLeader role</response>
    /// <response code="404">Group, schedule, or members not found</response>
    [HttpPost("groups/{idKey}/schedule-assignments")]
    [Authorize(Roles = "GroupLeader,Administrator")]
    [ProducesResponseType(typeof(List<ScheduleAssignmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAssignments(
        string idKey,
        [FromBody] CreateScheduleAssignmentsRequest request,
        CancellationToken ct = default)
    {
        if (request.MemberIdKeys == null || request.MemberIdKeys.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "At least one member must be specified",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (request.Dates == null || request.Dates.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "At least one date must be specified",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await volunteerScheduleService.CreateAssignmentsAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create schedule assignments: GroupIdKey={GroupIdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "UNPROCESSABLE_ENTITY" => UnprocessableEntity(new ProblemDetails
                {
                    Title = "Unprocessable entity",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                }),
                _ => Problem(
                    title: "Operation failed",
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    instance: HttpContext.Request.Path)
            };
        }

        logger.LogInformation(
            "Created {Count} schedule assignments for group {GroupIdKey}",
            result.Value!.Count, idKey);

        return CreatedAtAction(
            nameof(GetAssignments),
            new { idKey, startDate = request.Dates.Min(), endDate = request.Dates.Max() },
            result.Value);
    }

    /// <summary>
    /// Gets all schedule assignments for a group within a date range.
    /// </summary>
    /// <param name="idKey">The group's IdKey</param>
    /// <param name="startDate">Start of date range (inclusive)</param>
    /// <param name="endDate">End of date range (inclusive)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of assignments</returns>
    /// <response code="200">Returns assignments</response>
    /// <response code="400">Invalid date range</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("groups/{idKey}/schedule-assignments")]
    [ProducesResponseType(typeof(List<ScheduleAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAssignments(
        string idKey,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken ct = default)
    {
        if (endDate < startDate)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "End date must be greater than or equal to start date",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var assignments = await volunteerScheduleService.GetAssignmentsAsync(
            idKey,
            startDate,
            endDate,
            ct);

        logger.LogInformation(
            "Retrieved {Count} assignments for group {GroupIdKey} from {StartDate} to {EndDate}",
            assignments.Count, idKey, startDate, endDate);

        return Ok(assignments);
    }

    /// <summary>
    /// Updates the status of a schedule assignment (confirm or decline).
    /// Allows volunteers to respond to their assignments.
    /// </summary>
    /// <param name="assignmentIdKey">The assignment's IdKey</param>
    /// <param name="request">Status update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated assignment</returns>
    /// <response code="200">Status updated successfully</response>
    /// <response code="400">Validation failed (e.g., missing decline reason)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Assignment not found</response>
    [HttpPut("my-schedule/{assignmentIdKey}")]
    [ProducesResponseType(typeof(ScheduleAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAssignmentStatus(
        string assignmentIdKey,
        [FromBody] UpdateAssignmentStatusRequest request,
        CancellationToken ct = default)
    {
        // Get current user's person ID
        var currentPersonId = userContext.CurrentPersonId;
        if (currentPersonId == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "User not associated with a person record",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await volunteerScheduleService.UpdateAssignmentStatusAsync(
            assignmentIdKey,
            request,
            currentPersonId.Value,
            ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update assignment status: AssignmentIdKey={AssignmentIdKey}, Code={Code}, Message={Message}",
                assignmentIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "UNPROCESSABLE_ENTITY" => UnprocessableEntity(new ProblemDetails
                {
                    Title = "Unprocessable entity",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                }),
                _ => Problem(
                    title: "Operation failed",
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    instance: HttpContext.Request.Path)
            };
        }

        logger.LogInformation(
            "Updated assignment {AssignmentIdKey} status to {Status}",
            assignmentIdKey, request.Status);

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the current user's upcoming schedule assignments.
    /// Shows all assignments for the authenticated user.
    /// </summary>
    /// <param name="startDate">Optional start date (defaults to today)</param>
    /// <param name="endDate">Optional end date (defaults to 90 days from start)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of assignments grouped by date</returns>
    /// <response code="200">Returns schedule</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("my-schedule")]
    [ProducesResponseType(typeof(List<MyScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMySchedule(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken ct = default)
    {
        // Get person ID from authenticated user via UserContext
        var personId = userContext.CurrentPersonId;
        if (personId == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "User not associated with a person record",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var schedule = await volunteerScheduleService.GetMyScheduleAsync(
            personId.Value,
            startDate,
            endDate,
            ct);

        logger.LogInformation(
            "Retrieved schedule for person {PersonId}: {Count} dates",
            personId.Value, schedule.Count);

        return Ok(schedule);
    }
}
