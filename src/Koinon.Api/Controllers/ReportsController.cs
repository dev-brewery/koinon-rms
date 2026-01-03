using Koinon.Api.Filters;
using Koinon.Application.DTOs.Reports;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for report definition and execution operations.
/// Provides endpoints for managing report definitions and running/viewing reports.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
[ValidateIdKey]
public class ReportsController(
    IReportService reportService,
    ILogger<ReportsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all report definitions with optional filtering.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive report definitions</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of report definitions</returns>
    /// <response code="200">Returns paginated list of report definitions</response>
    [HttpGet("definitions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefinitions(
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

        var result = await reportService.GetDefinitionsAsync(includeInactive, page, pageSize, ct);

        logger.LogInformation(
            "Report definitions retrieved: IncludeInactive={IncludeInactive}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            includeInactive, page, pageSize, result.TotalCount);

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
    /// Gets a report definition by its IdKey.
    /// </summary>
    /// <param name="idKey">The report definition's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report definition details</returns>
    /// <response code="200">Returns report definition details</response>
    /// <response code="404">Report definition not found</response>
    [HttpGet("definitions/{idKey}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDefinition(string idKey, CancellationToken ct = default)
    {
        var definition = await reportService.GetDefinitionAsync(idKey, ct);

        if (definition == null)
        {
            logger.LogDebug("Report definition not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Report definition not found",
                Detail = $"No report definition found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Report definition retrieved: IdKey={IdKey}, Name={Name}", idKey, definition.Name);

        return Ok(new { data = definition });
    }

    /// <summary>
    /// Creates a new report definition.
    /// </summary>
    /// <param name="request">Report definition creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created report definition details</returns>
    /// <response code="201">Report definition created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost("definitions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateReportDefinitionRequest request, CancellationToken ct = default)
    {
        var result = await reportService.CreateDefinitionAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create report definition: Code={Code}, Message={Message}",
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

        var definition = result.Value!;

        logger.LogInformation(
            "Report definition created: IdKey={IdKey}, Name={Name}, Type={Type}",
            definition.IdKey,
            definition.Name,
            definition.ReportType);

        return CreatedAtAction(
            nameof(GetDefinition),
            new { idKey = definition.IdKey },
            new { data = definition });
    }

    /// <summary>
    /// Updates an existing report definition.
    /// </summary>
    /// <param name="idKey">The report definition's IdKey</param>
    /// <param name="request">Updated report definition details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated report definition details</returns>
    /// <response code="200">Report definition updated successfully</response>
    /// <response code="404">Report definition not found</response>
    /// <response code="422">Cannot update system report</response>
    [HttpPut("definitions/{idKey}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDefinition(
        string idKey,
        [FromBody] UpdateReportDefinitionRequest request,
        CancellationToken ct = default)
    {
        var result = await reportService.UpdateDefinitionAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update report definition {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
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

        logger.LogInformation("Report definition updated: IdKey={IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deletes a report definition (soft delete).
    /// </summary>
    /// <param name="idKey">The report definition's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Report definition deleted successfully</response>
    /// <response code="404">Report definition not found</response>
    /// <response code="422">Cannot delete system report</response>
    [HttpDelete("definitions/{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteDefinition(string idKey, CancellationToken ct = default)
    {
        var result = await reportService.DeleteDefinitionAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to delete report definition {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
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

        logger.LogInformation("Report definition deleted: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Runs a report and queues it for generation.
    /// </summary>
    /// <param name="request">Run request with report parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report run DTO containing execution status</returns>
    /// <response code="201">Report execution started successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Report definition not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost("run")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RunReport([FromBody] RunReportRequest request, CancellationToken ct = default)
    {
        var result = await reportService.RunReportAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to run report: Code={Code}, Message={Message}",
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

        var run = result.Value!;

        logger.LogInformation(
            "Report execution started: RunIdKey={RunIdKey}, DefinitionIdKey={DefinitionIdKey}, Status={Status}",
            run.IdKey,
            run.ReportDefinitionIdKey,
            run.Status);

        return CreatedAtAction(
            nameof(GetRun),
            new { idKey = run.IdKey },
            new { data = run });
    }

    /// <summary>
    /// Gets all report runs with optional filtering and pagination.
    /// </summary>
    /// <param name="reportDefinitionIdKey">Optional filter by report definition</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of report runs</returns>
    /// <response code="200">Returns paginated list of report runs</response>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuns(
        [FromQuery] string? reportDefinitionIdKey = null,
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

        var result = await reportService.GetRunsAsync(reportDefinitionIdKey, page, pageSize, ct);

        logger.LogInformation(
            "Report runs retrieved: DefinitionIdKey={DefinitionIdKey}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            reportDefinitionIdKey ?? "all", result.Page, result.PageSize, result.TotalCount);

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
    /// Gets a specific report run by IdKey.
    /// </summary>
    /// <param name="idKey">The report run's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report run details</returns>
    /// <response code="200">Returns report run details</response>
    /// <response code="404">Report run not found</response>
    [HttpGet("runs/{idKey}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(string idKey, CancellationToken ct = default)
    {
        var run = await reportService.GetRunAsync(idKey, ct);

        if (run == null)
        {
            logger.LogDebug("Report run not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Report run not found",
                Detail = $"No report run found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Report run retrieved: IdKey={IdKey}, Status={Status}", idKey, run.Status);

        return Ok(new { data = run });
    }

    /// <summary>
    /// Downloads a generated report file.
    /// </summary>
    /// <param name="idKey">The report run's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report file stream</returns>
    /// <response code="200">Returns file stream</response>
    /// <response code="404">Report run not found or file not ready</response>
    [HttpGet("runs/{idKey}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReport(string idKey, CancellationToken ct = default)
    {
        var result = await reportService.DownloadReportAsync(idKey, ct);

        if (result == null)
        {
            logger.LogDebug("Report file not found: RunIdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Report file not found",
                Detail = $"No report file found for run with IdKey '{idKey}'. The report may still be generating or may have failed.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var (stream, fileName, mimeType) = result.Value;

        logger.LogInformation(
            "Report file download started: RunIdKey={IdKey}, FileName={FileName}",
            idKey, fileName);

        // Return file stream with appropriate headers
        return File(stream, mimeType, fileName, enableRangeProcessing: true);
    }
}
