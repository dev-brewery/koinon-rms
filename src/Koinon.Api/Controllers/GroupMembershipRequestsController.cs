using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for managing group membership requests.
/// Provides endpoints for submitting, viewing, and processing membership requests.
/// </summary>
[ApiController]
[Route("api/v1/groups/{groupIdKey}/membership-requests")]
[Authorize]
[ValidateIdKey]
public class GroupMembershipRequestsController(
    IGroupMemberRequestService memberRequestService,
    ILogger<GroupMembershipRequestsController> logger) : ControllerBase
{
    /// <summary>
    /// Submits a membership request for the authenticated user to join a group.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey</param>
    /// <param name="request">Request details including optional note</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created membership request</returns>
    /// <response code="201">Membership request submitted successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="403">User is not allowed to request membership</response>
    /// <response code="404">Group not found</response>
    /// <response code="409">Request already exists for this user and group</response>
    [HttpPost]
    [ProducesResponseType(typeof(GroupMemberRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitRequest(
        string groupIdKey,
        [FromBody] SubmitMembershipRequestDto request,
        CancellationToken ct = default)
    {
        var result = await memberRequestService.SubmitRequestAsync(groupIdKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to submit membership request: GroupIdKey={GroupIdKey}, Code={Code}, Message={Message}",
                groupIdKey, result.Error!.Code, result.Error.Message);

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
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status403Forbidden,
                    Instance = HttpContext.Request.Path
                }),
                "CONFLICT" => Conflict(new ProblemDetails
                {
                    Title = "Request already exists",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status409Conflict,
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

        var memberRequest = result.Value!;

        logger.LogInformation(
            "Membership request submitted: IdKey={IdKey}, GroupIdKey={GroupIdKey}",
            memberRequest.IdKey, groupIdKey);

        return CreatedAtAction(
            nameof(GetPendingRequests),
            new { groupIdKey },
            memberRequest);
    }

    /// <summary>
    /// Gets all pending membership requests for a group.
    /// Only accessible by group leaders.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of pending membership requests</returns>
    /// <response code="200">Returns list of pending requests</response>
    /// <response code="403">User is not a group leader</response>
    /// <response code="404">Group not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupMemberRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingRequests(
        string groupIdKey,
        CancellationToken ct = default)
    {
        var result = await memberRequestService.GetPendingRequestsAsync(groupIdKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to get pending requests: GroupIdKey={GroupIdKey}, Code={Code}, Message={Message}",
                groupIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Group not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status403Forbidden,
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
            "Pending requests retrieved: GroupIdKey={GroupIdKey}, Count={Count}",
            groupIdKey, result.Value!.Count);

        return Ok(result.Value);
    }

    /// <summary>
    /// Processes (approves or denies) a membership request.
    /// Only accessible by group leaders.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey</param>
    /// <param name="requestIdKey">The request's IdKey</param>
    /// <param name="request">Processing details including status and optional note</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated membership request</returns>
    /// <response code="200">Request processed successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="403">User is not a group leader</response>
    /// <response code="404">Group or request not found</response>
    [HttpPut("{requestIdKey}")]
    [ProducesResponseType(typeof(GroupMemberRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessRequest(
        string groupIdKey,
        string requestIdKey,
        [FromBody] ProcessMembershipRequestDto request,
        CancellationToken ct = default)
    {
        var result = await memberRequestService.ProcessRequestAsync(groupIdKey, requestIdKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to process membership request: GroupIdKey={GroupIdKey}, RequestIdKey={RequestIdKey}, Code={Code}, Message={Message}",
                groupIdKey, requestIdKey, result.Error!.Code, result.Error.Message);

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
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status403Forbidden,
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

        var memberRequest = result.Value!;

        logger.LogInformation(
            "Membership request processed: IdKey={IdKey}, GroupIdKey={GroupIdKey}, Status={Status}",
            memberRequest.IdKey, groupIdKey, memberRequest.Status);

        return Ok(memberRequest);
    }
}
