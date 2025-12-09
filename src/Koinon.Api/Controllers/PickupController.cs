using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for pickup verification and checkout operations.
/// Handles child safety by verifying authorized pickup persons during checkout.
/// </summary>
[ApiController]
[Route("api/v1/checkin")]
[Authorize]
[ValidateIdKey]
public class PickupController(
    IAuthorizedPickupService authorizedPickupService,
    IPickupRateLimitService rateLimitService,
    ILogger<PickupController> logger) : ControllerBase
{
    /// <summary>
    /// Verifies if a person is authorized to pick up a child.
    /// Checks the authorized pickup list and authorization levels.
    /// Rate limited to 5 failed attempts per 15 minutes per attendance record and client IP
    /// to prevent brute-force attacks on 6-digit security codes.
    /// </summary>
    /// <param name="request">Pickup verification request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Verification result with authorization status</returns>
    /// <response code="200">Returns verification result (authorized or not)</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires CheckInVolunteer or Supervisor role</response>
    /// <response code="429">Too many failed attempts, rate limit exceeded</response>
    [HttpPost("verify-pickup")]
    [Authorize(Roles = "CheckInVolunteer,Supervisor")]
    [ProducesResponseType(typeof(PickupVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyPickup(
        [FromBody] VerifyPickupRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AttendanceIdKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "AttendanceIdKey is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (string.IsNullOrWhiteSpace(request.SecurityCode))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "SecurityCode is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Get client IP for rate limiting (uses ForwardedHeaders middleware)
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Check rate limit before processing request
        if (rateLimitService.IsRateLimited(request.AttendanceIdKey, clientIp))
        {
            var retryAfter = rateLimitService.GetRetryAfter(request.AttendanceIdKey, clientIp);
            var retryAfterSeconds = (int)(retryAfter?.TotalSeconds ?? 900); // Default to 15 minutes

            Response.Headers.Append("Retry-After", retryAfterSeconds.ToString());

            logger.LogWarning(
                "Rate limit exceeded for pickup verification: AttendanceIdKey={AttendanceIdKey}, ClientIP={ClientIp}",
                request.AttendanceIdKey, clientIp);

            return StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
            {
                Title = "Too Many Requests",
                Detail = "Too many failed pickup verification attempts. Please wait before trying again.",
                Status = StatusCodes.Status429TooManyRequests,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await authorizedPickupService.VerifyPickupAsync(request, ct);

        // Track rate limiting based on verification result
        if (!result.IsAuthorized && !result.RequiresSupervisorOverride)
        {
            // Record failed attempt for invalid security code or "Never" authorization
            rateLimitService.RecordFailedAttempt(request.AttendanceIdKey, clientIp);
        }
        else if (result.IsAuthorized)
        {
            // Reset counter on successful verification
            rateLimitService.ResetAttempts(request.AttendanceIdKey, clientIp);
        }
        // Note: Don't track "RequiresSupervisorOverride" cases (emergency-only, not on list)
        // as these are legitimate scenarios, not brute-force attempts

        logger.LogInformation(
            "Pickup verification completed: AttendanceIdKey={AttendanceIdKey}, IsAuthorized={IsAuthorized}, RequiresOverride={RequiresOverride}",
            request.AttendanceIdKey, result.IsAuthorized, result.RequiresSupervisorOverride);

        return Ok(result);
    }

    /// <summary>
    /// Records a pickup event in the audit log and checks out the child.
    /// </summary>
    /// <param name="request">Pickup recording request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created pickup log entry</returns>
    /// <response code="201">Pickup recorded successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires CheckInVolunteer or Supervisor role</response>
    [HttpPost("record-pickup")]
    [Authorize(Roles = "CheckInVolunteer,Supervisor")]
    [ProducesResponseType(typeof(PickupLogDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordPickup(
        [FromBody] RecordPickupRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AttendanceIdKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "AttendanceIdKey is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // CRITICAL #3: Verify supervisor role if supervisor override is requested
        if (request.SupervisorOverride)
        {
            if (!User.IsInRole("Supervisor"))
            {
                return Problem(
                    detail: "Supervisor override requires a user with Supervisor role",
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden");
            }
        }

        var pickupLog = await authorizedPickupService.RecordPickupAsync(request, ct);

        logger.LogInformation(
            "Pickup recorded: AttendanceIdKey={AttendanceIdKey}, PickupPerson={PickupPerson}, WasAuthorized={WasAuthorized}, SupervisorOverride={SupervisorOverride}",
            request.AttendanceIdKey,
            request.PickupPersonName ?? "Unknown",
            request.WasAuthorized,
            request.SupervisorOverride);

        return CreatedAtAction(
            nameof(GetPickupHistory),
            new { childIdKey = "placeholder" }, // Will be determined from attendance
            pickupLog);
    }

    /// <summary>
    /// Gets the pickup history for a child.
    /// </summary>
    /// <param name="childIdKey">The child's IdKey</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of pickup log entries</returns>
    /// <response code="200">Returns pickup history</response>
    /// <response code="400">Invalid IdKey format or date range</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires Supervisor role</response>
    [HttpGet("people/{childIdKey}/pickup-history")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(List<PickupLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPickupHistory(
        string childIdKey,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default)
    {
        if (toDate.HasValue && fromDate.HasValue && toDate.Value < fromDate.Value)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date range",
                Detail = "The 'toDate' must be greater than or equal to 'fromDate'",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var history = await authorizedPickupService.GetPickupHistoryAsync(
            childIdKey,
            fromDate,
            toDate,
            ct);

        logger.LogInformation(
            "Pickup history retrieved: ChildIdKey={ChildIdKey}, Count={Count}, FromDate={FromDate}, ToDate={ToDate}",
            childIdKey, history.Count, fromDate, toDate);

        return Ok(history);
    }
}
