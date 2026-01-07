using System.Security.Claims;
using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for parent paging operations during check-in.
/// Manages sending SMS notifications to parents when their child needs attention.
/// </summary>
[ApiController]
[Route("api/v1/pager")]
[Authorize]
[ValidateIdKey]
public class PagerController(
    IParentPagingService parentPagingService,
    ILogger<PagerController> logger) : ControllerBase
{
    /// <summary>
    /// Sends a page to a parent via SMS.
    /// </summary>
    /// <param name="request">Page request with pager number, message type, and optional custom message</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Pager message details</returns>
    /// <response code="200">Page sent successfully</response>
    /// <response code="400">Validation error or rate limit exceeded</response>
    /// <response code="401">Not authenticated or not authorized</response>
    /// <response code="404">Pager number not found</response>
    [HttpPost("send")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(PagerMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendPage(
        [FromBody] SendPageRequest request,
        CancellationToken ct = default)
    {
        // Get the current user's PersonId from claims
        var personIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(personIdClaim) || !int.TryParse(personIdClaim, out var sentByPersonId))
        {
            logger.LogWarning("Failed to get PersonId from claims for pager send");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication error",
                Detail = "Unable to identify current user",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await parentPagingService.SendPageAsync(request, sentByPersonId, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to send page: PagerNumber={PagerNumber}, Code={Code}, Message={Message}",
                request.PagerNumber, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Pager not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "RATE_LIMIT_EXCEEDED" => BadRequest(new ProblemDetails
                {
                    Title = "Rate limit exceeded",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
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
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Unexpected error",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        var message = result.Value!;

        logger.LogInformation(
            "Page sent successfully: PagerNumber={PagerNumber}, MessageType={MessageType}, SentBy={SentByPersonId}",
            request.PagerNumber, request.MessageType, sentByPersonId);

        return Ok(new { data = message });
    }

    /// <summary>
    /// Searches for pager assignments by pager number or child name.
    /// </summary>
    /// <param name="searchTerm">Optional search term (pager number or child name)</param>
    /// <param name="date">Optional date filter (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching pager assignments</returns>
    /// <response code="200">Returns list of matching pager assignments</response>
    /// <response code="401">Not authenticated or not authorized</response>
    [HttpGet("search")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(List<PagerAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchPagers(
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date,
        CancellationToken ct = default)
    {
        var searchRequest = new PageSearchRequest(
            searchTerm,
            CampusId: null, // Not filtering by campus in this endpoint
            date ?? DateTime.UtcNow.Date);

        var assignments = await parentPagingService.SearchPagerAsync(searchRequest, ct);

        logger.LogInformation(
            "Pager search completed: SearchTerm={SearchTerm}, Date={Date}, ResultCount={Count}",
            searchTerm ?? "All", date?.ToString("yyyy-MM-dd") ?? "Today", assignments.Count);

        return Ok(new { data = assignments });
    }

    /// <summary>
    /// Gets page history for a specific pager number.
    /// </summary>
    /// <param name="pagerNumber">The numeric pager number (e.g., 127)</param>
    /// <param name="date">Optional date filter (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Page history with all messages for this pager</returns>
    /// <response code="200">Returns page history</response>
    /// <response code="401">Not authenticated or not authorized</response>
    /// <response code="404">Pager number not found or no history for date</response>
    [HttpGet("{pagerNumber}/history")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(PageHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageHistory(
        int pagerNumber,
        [FromQuery] DateTime? date,
        CancellationToken ct = default)
    {
        var history = await parentPagingService.GetPageHistoryAsync(pagerNumber, date, ct);

        if (history == null)
        {
            logger.LogDebug(
                "Page history not found: PagerNumber={PagerNumber}, Date={Date}",
                pagerNumber, date?.ToString("yyyy-MM-dd") ?? "Today");

            return NotFound(new ProblemDetails
            {
                Title = "Page history not found",
                Detail = $"No page history found for pager {pagerNumber} on {date?.ToString("yyyy-MM-dd") ?? "today"}",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "Page history retrieved: PagerNumber={PagerNumber}, Date={Date}, MessageCount={MessageCount}",
            pagerNumber, date?.ToString("yyyy-MM-dd") ?? "Today", history.Messages.Count);

        return Ok(new { data = history });
    }

    /// <summary>
    /// Gets the next available pager number for a campus.
    /// Used during check-in to assign pager numbers sequentially.
    /// Restricted to Supervisor role.
    /// </summary>
    /// <param name="campusId">Optional campus IdKey for scoping</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Next available pager number (starting at 100)</returns>
    /// <response code="200">Returns next available pager number</response>
    /// <response code="401">Not authenticated or not authorized</response>
    [HttpGet("next-number")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNextPagerNumber(
        [FromQuery] string? campusId,
        CancellationToken ct = default)
    {
        // Decode campusId if provided (ValidateIdKey filter will have already validated format)
        int? campusIdInt = null;
        if (!string.IsNullOrEmpty(campusId))
        {
            if (Koinon.Domain.Data.IdKeyHelper.TryDecode(campusId, out var decodedId))
            {
                campusIdInt = decodedId;
            }
        }

        var nextNumber = await parentPagingService.GetNextPagerNumberAsync(
            campusIdInt,
            DateTime.UtcNow.Date,
            ct);

        logger.LogDebug(
            "Next pager number retrieved: CampusId={CampusId}, NextNumber={NextNumber}",
            campusId ?? "All", nextNumber);

        return Ok(new { data = nextNumber });
    }
}
