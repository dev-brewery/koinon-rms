using Koinon.Api.Filters;
using Koinon.Application.DTOs.Exports;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for data export operations.
/// Provides endpoints for creating, monitoring, and downloading data exports.
/// </summary>
[ApiController]
[Route("api/v1/exports")]
[Authorize]
[ValidateIdKey]
public class ExportsController(
    IDataExportService dataExportService,
    ILogger<ExportsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all export jobs with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of export jobs</returns>
    /// <response code="200">Returns paginated list of export jobs</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExports(
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

        var result = await dataExportService.GetExportJobsAsync(page, pageSize, ct);

        logger.LogInformation(
            "Export jobs retrieved: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            page, pageSize, result.TotalCount);

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
    /// Gets a single export job by its IdKey.
    /// </summary>
    /// <param name="idKey">The export job's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Export job details</returns>
    /// <response code="200">Returns export job details</response>
    /// <response code="404">Export job not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExport(string idKey, CancellationToken ct = default)
    {
        var exportJob = await dataExportService.GetExportJobAsync(idKey, ct);

        if (exportJob == null)
        {
            logger.LogDebug("Export job not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Export job not found",
                Detail = $"No export job found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug(
            "Export job retrieved: IdKey={IdKey}, Status={Status}, Type={Type}",
            idKey, exportJob.Status, exportJob.ExportType);

        return Ok(new { data = exportJob });
    }

    /// <summary>
    /// Starts a new data export job and queues it for processing.
    /// </summary>
    /// <param name="request">Export request with type, format, and parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created export job details</returns>
    /// <response code="201">Export job created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> StartExport([FromBody] StartExportRequest request, CancellationToken ct = default)
    {
        var result = await dataExportService.StartExportAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to start export: Code={Code}, Message={Message}",
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

        var exportJob = result.Value!;

        logger.LogInformation(
            "Export job created: IdKey={IdKey}, Type={Type}, Format={Format}, Status={Status}",
            exportJob.IdKey,
            exportJob.ExportType,
            exportJob.OutputFormat,
            exportJob.Status);

        return CreatedAtAction(
            nameof(GetExport),
            new { idKey = exportJob.IdKey },
            new { data = exportJob });
    }

    /// <summary>
    /// Downloads the generated export file for a completed export job.
    /// </summary>
    /// <param name="idKey">The export job's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Export file stream</returns>
    /// <response code="200">Returns file stream</response>
    /// <response code="404">Export job not found or file not ready</response>
    [HttpGet("{idKey}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadExport(string idKey, CancellationToken ct = default)
    {
        var result = await dataExportService.DownloadExportAsync(idKey, ct);

        if (result == null)
        {
            logger.LogDebug("Export file not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Export file not found",
                Detail = $"No export file found for job with IdKey '{idKey}'. The export may still be processing or may have failed.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var (stream, fileName, mimeType) = result.Value;

        logger.LogInformation(
            "Export file download started: IdKey={IdKey}, FileName={FileName}",
            idKey, fileName);

        // Return file stream with appropriate headers
        return File(stream, mimeType, fileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// Gets the list of available fields for a specific export type.
    /// </summary>
    /// <param name="exportType">The export type to get fields for</param>
    /// <returns>List of available export fields</returns>
    /// <response code="200">Returns list of available fields</response>
    /// <response code="400">Invalid export type</response>
    [HttpGet("fields/{exportType}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetAvailableFields(ExportType exportType)
    {
        // Validate enum value
        if (!Enum.IsDefined(typeof(ExportType), exportType))
        {
            logger.LogWarning("Invalid export type requested: {ExportType}", exportType);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid export type",
                Detail = $"The export type '{exportType}' is not valid. Valid values are: {string.Join(", ", Enum.GetNames(typeof(ExportType)))}",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var fields = dataExportService.GetAvailableFields(exportType);

        logger.LogDebug(
            "Available export fields retrieved: ExportType={ExportType}, FieldCount={FieldCount}",
            exportType, fields.Count);

        return Ok(new { data = fields });
    }
}
