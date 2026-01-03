using Koinon.Api.Filters;
using Koinon.Application.DTOs.Reports;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for scheduled report operations.
/// Provides endpoints for managing automated report generation schedules.
/// </summary>
[ApiController]
[Route("api/v1/reports/schedules")]
[Authorize]
[ValidateIdKey]
public class ReportSchedulesController(
    IReportScheduleService reportScheduleService,
    ILogger<ReportSchedulesController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all report schedules with optional filtering and pagination.
    /// </summary>
    /// <param name="reportDefinitionIdKey">Optional filter by report definition</param>
    /// <param name="includeInactive">Whether to include inactive schedules</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of report schedules</returns>
    /// <response code="200">Returns paginated list of report schedules</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] string? reportDefinitionIdKey = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 25;
        }

        var result = await reportScheduleService.GetSchedulesAsync(
            reportDefinitionIdKey,
            includeInactive,
            page,
            pageSize,
            ct);

        logger.LogInformation(
            "Report schedules retrieved: DefinitionIdKey={DefinitionIdKey}, IncludeInactive={IncludeInactive}, TotalCount={TotalCount}",
            reportDefinitionIdKey ?? "all", includeInactive, result.TotalCount);

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
    /// Gets a report schedule by its IdKey.
    /// </summary>
    /// <param name="idKey">The report schedule's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report schedule details</returns>
    /// <response code="200">Returns report schedule details</response>
    /// <response code="404">Report schedule not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(string idKey, CancellationToken ct = default)
    {
        var schedule = await reportScheduleService.GetScheduleAsync(idKey, ct);

        if (schedule == null)
        {
            logger.LogDebug("Report schedule not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Report schedule not found",
                Detail = $"No report schedule found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Report schedule retrieved: IdKey={IdKey}, ReportName={ReportName}", idKey, schedule.ReportName);

        return Ok(new { data = schedule });
    }

    /// <summary>
    /// Creates a new report schedule.
    /// </summary>
    /// <param name="request">Report schedule creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created report schedule details</returns>
    /// <response code="201">Report schedule created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Report definition not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateReportScheduleRequest request, CancellationToken ct = default)
    {
        var result = await reportScheduleService.CreateScheduleAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create report schedule: Code={Code}, Message={Message}",
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
                    Title = "Report definition not found",
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

        var schedule = result.Value!;

        logger.LogInformation(
            "Report schedule created: IdKey={IdKey}, ReportName={ReportName}, CronExpression={CronExpression}",
            schedule.IdKey,
            schedule.ReportName,
            schedule.CronExpression);

        return CreatedAtAction(
            nameof(GetSchedule),
            new { idKey = schedule.IdKey },
            new { data = schedule });
    }

    /// <summary>
    /// Updates an existing report schedule.
    /// </summary>
    /// <param name="idKey">The report schedule's IdKey</param>
    /// <param name="request">Updated report schedule details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated report schedule details</returns>
    /// <response code="200">Report schedule updated successfully</response>
    /// <response code="404">Report schedule not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSchedule(
        string idKey,
        [FromBody] UpdateReportScheduleRequest request,
        CancellationToken ct = default)
    {
        var result = await reportScheduleService.UpdateScheduleAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update report schedule {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Report schedule not found",
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

        logger.LogInformation("Report schedule updated: IdKey={IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deletes a report schedule.
    /// </summary>
    /// <param name="idKey">The report schedule's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Report schedule deleted successfully</response>
    /// <response code="404">Report schedule not found</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSchedule(string idKey, CancellationToken ct = default)
    {
        var result = await reportScheduleService.DeleteScheduleAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to delete report schedule {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Report schedule not found",
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

        logger.LogInformation("Report schedule deleted: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Manually triggers a scheduled report to run immediately.
    /// </summary>
    /// <param name="idKey">The report schedule's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report run DTO containing execution status</returns>
    /// <response code="201">Scheduled report triggered successfully</response>
    /// <response code="404">Report schedule not found</response>
    /// <response code="422">Business rule violation (e.g., inactive schedule)</response>
    [HttpPost("{idKey}/trigger")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TriggerScheduledReport(string idKey, CancellationToken ct = default)
    {
        var result = await reportScheduleService.TriggerScheduledReportAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to trigger scheduled report {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Report schedule not found",
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

        var run = result.Value!;

        logger.LogInformation(
            "Scheduled report triggered: ScheduleIdKey={ScheduleIdKey}, RunIdKey={RunIdKey}, Status={Status}",
            idKey,
            run.IdKey,
            run.Status);

        return CreatedAtAction(
            "GetRun",
            "Reports",
            new { idKey = run.IdKey },
            new { data = run });
    }
}
