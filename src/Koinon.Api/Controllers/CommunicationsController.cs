using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for communication management operations.
/// Provides endpoints for creating, managing, and sending email and SMS communications.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ValidateIdKey]
public class CommunicationsController(
    ICommunicationService communicationService,
    ILogger<CommunicationsController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for communications with optional pagination and status filter.
    /// </summary>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="status">Filter by status (Draft, Pending, Sent, Failed)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of communications</returns>
    /// <response code="200">Returns paginated list of communications</response>
    [HttpGet]
    [ProducesResponseType(typeof(Application.Common.PagedResult<CommunicationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 20;
        }

        var result = await communicationService.SearchAsync(page, pageSize, status, ct);

        logger.LogInformation(
            "Communication search completed: Page={Page}, PageSize={PageSize}, Status={Status}, TotalCount={TotalCount}",
            result.Page, result.PageSize, status ?? "all", result.TotalCount);

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
    /// Gets a communication by its IdKey with full details including recipients.
    /// </summary>
    /// <param name="idKey">The communication's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Communication details</returns>
    /// <response code="200">Returns communication details</response>
    /// <response code="404">Communication not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(CommunicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var communication = await communicationService.GetByIdKeyAsync(idKey, ct);

        if (communication == null)
        {
            logger.LogDebug("Communication not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Communication not found",
                Detail = $"No communication found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Communication retrieved: IdKey={IdKey}, Type={Type}", idKey, communication.CommunicationType);

        return Ok(new { data = communication });
    }

    /// <summary>
    /// Creates a new communication.
    /// </summary>
    /// <param name="dto">Communication creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created communication details</returns>
    /// <response code="201">Communication created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(CommunicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateCommunicationDto dto, CancellationToken ct = default)
    {
        var result = await communicationService.CreateAsync(dto, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create communication: Code={Code}, Message={Message}",
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

        var communication = result.Value!;

        logger.LogInformation(
            "Communication created: IdKey={IdKey}, Type={Type}, Recipients={RecipientCount}",
            communication.IdKey,
            communication.CommunicationType,
            communication.RecipientCount);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = communication.IdKey },
            communication);
    }

    /// <summary>
    /// Updates an existing communication (only allowed if status is Draft).
    /// </summary>
    /// <param name="idKey">The communication's IdKey</param>
    /// <param name="dto">Updated communication details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated communication details</returns>
    /// <response code="200">Communication updated successfully</response>
    /// <response code="404">Communication not found</response>
    /// <response code="422">Cannot update non-draft communication</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(CommunicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateCommunicationDto dto,
        CancellationToken ct = default)
    {
        var result = await communicationService.UpdateAsync(idKey, dto, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update communication {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Communication not found",
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

        logger.LogInformation("Communication updated: IdKey={IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deletes a communication (only allowed if status is Draft).
    /// </summary>
    /// <param name="idKey">The communication's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Communication deleted successfully</response>
    /// <response code="404">Communication not found</response>
    /// <response code="422">Cannot delete non-draft communication</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await communicationService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to delete communication {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Communication not found",
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

        logger.LogInformation("Communication deleted: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Queues a communication for sending (changes status from Draft to Pending).
    /// </summary>
    /// <param name="idKey">The communication's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated communication details</returns>
    /// <response code="200">Communication queued for sending</response>
    /// <response code="404">Communication not found</response>
    /// <response code="422">Cannot send non-draft communication</response>
    [HttpPost("{idKey}/send")]
    [ProducesResponseType(typeof(CommunicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Send(string idKey, CancellationToken ct = default)
    {
        var result = await communicationService.SendAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to send communication {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Communication not found",
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
            "Communication queued for sending: IdKey={IdKey}, Recipients={RecipientCount}",
            idKey,
            result.Value!.RecipientCount);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Schedules a communication to be sent at a future date and time.
    /// </summary>
    /// <param name="idKey">The communication's IdKey</param>
    /// <param name="request">Schedule request containing the scheduled date and time</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated communication details</returns>
    /// <response code="200">Communication scheduled successfully</response>
    /// <response code="400">Invalid scheduled date/time</response>
    /// <response code="404">Communication not found</response>
    /// <response code="422">Cannot schedule non-draft communication</response>
    [HttpPost("{idKey}/schedule")]
    [ProducesResponseType(typeof(CommunicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Schedule(
        string idKey,
        [FromBody] ScheduleCommunicationRequest request,
        CancellationToken ct = default)
    {
        var result = await communicationService.ScheduleAsync(idKey, request.ScheduledDateTime, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to schedule communication {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Communication not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = "Invalid scheduled date/time",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
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
            "Communication scheduled: IdKey={IdKey}, ScheduledDateTime={ScheduledDateTime}",
            idKey,
            result.Value!.ScheduledDateTime);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Cancels a scheduled communication and reverts it to Draft status.
    /// </summary>
    /// <param name="idKey">The communication's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated communication details</returns>
    /// <response code="200">Schedule cancelled successfully</response>
    /// <response code="404">Communication not found</response>
    /// <response code="422">Cannot cancel schedule for non-scheduled communication</response>
    [HttpPost("{idKey}/cancel-schedule")]
    [ProducesResponseType(typeof(CommunicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelSchedule(string idKey, CancellationToken ct = default)
    {
        var result = await communicationService.CancelScheduleAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to cancel schedule for communication {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Communication not found",
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
            "Communication schedule cancelled: IdKey={IdKey}",
            idKey);

        return Ok(new { data = result.Value });
    }
}

/// <summary>
/// Request DTO for scheduling a communication.
/// </summary>
public record ScheduleCommunicationRequest(DateTime ScheduledDateTime);
