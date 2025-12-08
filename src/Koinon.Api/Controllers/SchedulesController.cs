using Koinon.Api.Helpers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for schedule management operations.
/// Provides endpoints for managing service times and check-in schedules.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SchedulesController(
    IScheduleService scheduleService,
    ILogger<SchedulesController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for schedules with optional filters and pagination.
    /// </summary>
    /// <param name="query">Full-text search query</param>
    /// <param name="dayOfWeek">Filter by day of week (0=Sunday, 6=Saturday)</param>
    /// <param name="includeInactive">Include inactive schedules</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of schedules</returns>
    /// <response code="200">Returns paginated list of schedules</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ScheduleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var parameters = new ScheduleSearchParameters
        {
            Query = query,
            DayOfWeek = dayOfWeek,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize
        };

        var result = await scheduleService.SearchAsync(parameters, ct);

        logger.LogInformation(
            "Schedule search completed: Query={Query}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            query, result.Page, result.PageSize, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Gets a schedule by its IdKey with full details.
    /// </summary>
    /// <param name="idKey">The schedule's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Schedule details</returns>
    /// <response code="200">Returns schedule details</response>
    /// <response code="404">Schedule not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
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

        var schedule = await scheduleService.GetByIdKeyAsync(idKey, ct);

        if (schedule == null)
        {
            logger.LogWarning("Schedule not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Schedule not found",
                Detail = $"No schedule found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Schedule retrieved: IdKey={IdKey}, Name={Name}", idKey, schedule.Name);

        return Ok(schedule);
    }

    /// <summary>
    /// Creates a new schedule.
    /// </summary>
    /// <param name="request">Schedule creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created schedule details</returns>
    /// <response code="201">Schedule created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request, CancellationToken ct = default)
    {
        var result = await scheduleService.CreateAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create schedule: Code={Code}, Message={Message}",
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

        var schedule = result.Value!;

        logger.LogInformation(
            "Schedule created successfully: IdKey={IdKey}, Name={Name}",
            schedule.IdKey, schedule.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = schedule.IdKey },
            schedule);
    }

    /// <summary>
    /// Updates an existing schedule.
    /// </summary>
    /// <param name="idKey">The schedule's IdKey</param>
    /// <param name="request">Schedule update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated schedule details</returns>
    /// <response code="200">Schedule updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Schedule not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateScheduleRequest request,
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

        var result = await scheduleService.UpdateAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update schedule: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Schedule not found",
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

        var schedule = result.Value!;

        logger.LogInformation(
            "Schedule updated successfully: IdKey={IdKey}, Name={Name}",
            schedule.IdKey, schedule.Name);

        return Ok(schedule);
    }

    /// <summary>
    /// Deactivates a schedule (soft delete).
    /// </summary>
    /// <param name="idKey">The schedule's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Schedule deactivated successfully</response>
    /// <response code="404">Schedule not found</response>
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

        var result = await scheduleService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to deactivate schedule: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Schedule not found",
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

        logger.LogInformation("Schedule deactivated successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Gets upcoming occurrences for a schedule.
    /// </summary>
    /// <param name="idKey">The schedule's IdKey</param>
    /// <param name="startDate">Start date for occurrence calculation (defaults to today)</param>
    /// <param name="count">Number of occurrences to return (default 10, max 52)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of schedule occurrences</returns>
    /// <response code="200">Returns list of occurrences</response>
    /// <response code="400">Invalid IdKey format</response>
    [HttpGet("{idKey}/occurrences")]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduleOccurrenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOccurrences(
        string idKey,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] int count = 10,
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

        if (count < 1 || count > 52)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid count parameter",
                Detail = "Count must be between 1 and 52",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var occurrences = await scheduleService.GetOccurrencesAsync(idKey, startDate, count, ct);

        logger.LogInformation(
            "Schedule occurrences retrieved: IdKey={IdKey}, Count={Count}",
            idKey, occurrences.Count);

        return Ok(occurrences);
    }
}
