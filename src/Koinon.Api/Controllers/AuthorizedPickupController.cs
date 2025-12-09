using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for managing authorized pickup persons for children.
/// Handles creation, retrieval, updates, and deletion of authorized pickup records.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[ValidateIdKey]
public class AuthorizedPickupController(
    IAuthorizedPickupService authorizedPickupService,
    ILogger<AuthorizedPickupController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all authorized pickup persons for a child.
    /// CRITICAL #2: Restricted to supervisors only to protect custody information.
    /// </summary>
    /// <param name="childIdKey">The child's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of authorized pickup persons</returns>
    /// <response code="200">Returns list of authorized pickup persons</response>
    /// <response code="400">Invalid IdKey format</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires Supervisor role</response>
    [HttpGet("people/{childIdKey}/authorized-pickups")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(List<AuthorizedPickupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuthorizedPickups(
        string childIdKey,
        CancellationToken ct = default)
    {
        var pickups = await authorizedPickupService.GetAuthorizedPickupsAsync(childIdKey, ct);

        logger.LogInformation(
            "Authorized pickups retrieved: ChildIdKey={ChildIdKey}, Count={Count}",
            childIdKey, pickups.Count);

        return Ok(pickups);
    }

    /// <summary>
    /// Adds a new authorized pickup person for a child.
    /// </summary>
    /// <param name="childIdKey">The child's IdKey</param>
    /// <param name="request">Authorized pickup creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created authorized pickup details</returns>
    /// <response code="201">Authorized pickup created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires Supervisor role</response>
    [HttpPost("people/{childIdKey}/authorized-pickups")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(AuthorizedPickupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAuthorizedPickup(
        string childIdKey,
        [FromBody] CreateAuthorizedPickupRequest request,
        CancellationToken ct = default)
    {
        var pickup = await authorizedPickupService.AddAuthorizedPickupAsync(childIdKey, request, ct);

        logger.LogInformation(
            "Authorized pickup created: ChildIdKey={ChildIdKey}, PickupIdKey={PickupIdKey}, Name={Name}",
            childIdKey, pickup.IdKey, pickup.Name ?? pickup.AuthorizedPersonName);

        return CreatedAtAction(
            nameof(GetAuthorizedPickups),
            new { childIdKey },
            pickup);
    }

    /// <summary>
    /// Updates an existing authorized pickup person.
    /// </summary>
    /// <param name="pickupIdKey">The authorized pickup's IdKey</param>
    /// <param name="request">Authorized pickup update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated authorized pickup details</returns>
    /// <response code="200">Authorized pickup updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires Supervisor role</response>
    /// <response code="404">Authorized pickup not found</response>
    [HttpPut("authorized-pickups/{pickupIdKey}")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(AuthorizedPickupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAuthorizedPickup(
        string pickupIdKey,
        [FromBody] UpdateAuthorizedPickupRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var pickup = await authorizedPickupService.UpdateAuthorizedPickupAsync(pickupIdKey, request, ct);

            logger.LogInformation(
                "Authorized pickup updated: PickupIdKey={PickupIdKey}, Name={Name}",
                pickupIdKey, pickup.Name ?? pickup.AuthorizedPersonName);

            return Ok(pickup);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to update authorized pickup: PickupIdKey={PickupIdKey}",
                pickupIdKey);

            return NotFound(new ProblemDetails
            {
                Title = "Authorized pickup not found",
                Detail = $"No authorized pickup found with IdKey '{pickupIdKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Deletes (deactivates) an authorized pickup person.
    /// </summary>
    /// <param name="pickupIdKey">The authorized pickup's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Authorized pickup deleted successfully</response>
    /// <response code="400">Invalid IdKey format</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires Supervisor role</response>
    /// <response code="404">Authorized pickup not found</response>
    [HttpDelete("authorized-pickups/{pickupIdKey}")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAuthorizedPickup(
        string pickupIdKey,
        CancellationToken ct = default)
    {
        try
        {
            await authorizedPickupService.DeleteAuthorizedPickupAsync(pickupIdKey, ct);

            logger.LogInformation(
                "Authorized pickup deleted: PickupIdKey={PickupIdKey}",
                pickupIdKey);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to delete authorized pickup: PickupIdKey={PickupIdKey}",
                pickupIdKey);

            return NotFound(new ProblemDetails
            {
                Title = "Authorized pickup not found",
                Detail = $"No authorized pickup found with IdKey '{pickupIdKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Auto-populates the authorized pickup list with adult family members.
    /// Adds parents and guardians from the child's family as authorized pickups.
    /// </summary>
    /// <param name="childIdKey">The child's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Family members added as authorized pickups</response>
    /// <response code="400">Invalid IdKey format</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Requires Supervisor role</response>
    [HttpPost("people/{childIdKey}/authorized-pickups/auto-populate")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AutoPopulateFamilyMembers(
        string childIdKey,
        CancellationToken ct = default)
    {
        await authorizedPickupService.AutoPopulateFamilyMembersAsync(childIdKey, ct);

        var pickups = await authorizedPickupService.GetAuthorizedPickupsAsync(childIdKey, ct);

        logger.LogInformation(
            "Auto-populated authorized pickups: ChildIdKey={ChildIdKey}, Count={Count}",
            childIdKey, pickups.Count);

        return Ok(new
        {
            message = "Family members successfully added as authorized pickups",
            count = pickups.Count,
            pickups
        });
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
