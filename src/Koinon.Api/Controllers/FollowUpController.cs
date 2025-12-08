using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for managing follow-up tasks for visitors and attendees.
/// Provides endpoints for retrieving, updating, and assigning follow-ups.
/// </summary>
[ApiController]
[Route("api/v1/followups")]
[Authorize]
[ValidateIdKey]
public class FollowUpController(
    IFollowUpService followUpService,
    ILogger<FollowUpController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all pending follow-ups, optionally filtered by assignee.
    /// </summary>
    /// <param name="assignedToIdKey">Optional IdKey of the person assigned to the follow-ups</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of pending follow-ups</returns>
    /// <response code="200">Returns list of pending follow-ups</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<FollowUpDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPending(
        [FromQuery] string? assignedToIdKey = null,
        CancellationToken ct = default)
    {
        // Validate assignedToIdKey if provided
        if (!string.IsNullOrEmpty(assignedToIdKey) && !IdKeyHelper.TryDecode(assignedToIdKey, out _))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey",
                Detail = $"'{assignedToIdKey}' is not a valid IdKey format",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var followUps = await followUpService.GetPendingFollowUpsAsync(assignedToIdKey, ct);

        logger.LogInformation(
            "Pending follow-ups retrieved: Count={Count}, AssignedTo={AssignedToIdKey}",
            followUps.Count, assignedToIdKey ?? "All");

        return Ok(followUps);
    }

    /// <summary>
    /// Gets a follow-up by its IdKey.
    /// </summary>
    /// <param name="idKey">The follow-up's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Follow-up details</returns>
    /// <response code="200">Returns follow-up details</response>
    /// <response code="404">Follow-up not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(FollowUpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var followUp = await followUpService.GetByIdKeyAsync(idKey, ct);

        if (followUp == null)
        {
            logger.LogDebug("Follow-up not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Follow-up not found",
                Detail = $"No follow-up found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Follow-up retrieved: IdKey={IdKey}, Person={PersonName}", idKey, followUp.PersonName);

        return Ok(followUp);
    }

    /// <summary>
    /// Updates the status of a follow-up task.
    /// </summary>
    /// <param name="idKey">The follow-up's IdKey</param>
    /// <param name="request">Status update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated follow-up details</returns>
    /// <response code="200">Follow-up status updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Follow-up not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}/status")]
    [ProducesResponseType(typeof(FollowUpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        string idKey,
        [FromBody] UpdateFollowUpStatusRequest request,
        CancellationToken ct = default)
    {
        var result = await followUpService.UpdateStatusAsync(idKey, request.Status, request.Notes, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update follow-up status: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Follow-up not found",
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

        var followUp = result.Value!;

        logger.LogInformation(
            "Follow-up status updated successfully: IdKey={IdKey}, Status={Status}",
            idKey, request.Status);

        return Ok(followUp);
    }

    /// <summary>
    /// Assigns a follow-up task to a person.
    /// </summary>
    /// <param name="idKey">The follow-up's IdKey</param>
    /// <param name="request">Assignment details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="200">Follow-up assigned successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Follow-up or person not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(
        string idKey,
        [FromBody] AssignFollowUpRequest request,
        CancellationToken ct = default)
    {
        // Validate AssignedToIdKey from request body
        if (!IdKeyHelper.TryDecode(request.AssignedToIdKey, out _))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey",
                Detail = $"'{request.AssignedToIdKey}' is not a valid IdKey format",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await followUpService.AssignFollowUpAsync(idKey, request.AssignedToIdKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to assign follow-up: IdKey={IdKey}, AssignedTo={AssignedToIdKey}, Code={Code}, Message={Message}",
                idKey, request.AssignedToIdKey, result.Error!.Code, result.Error.Message);

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

        logger.LogInformation(
            "Follow-up assigned successfully: IdKey={IdKey}, AssignedTo={AssignedToIdKey}",
            idKey, request.AssignedToIdKey);

        return Ok();
    }
}
