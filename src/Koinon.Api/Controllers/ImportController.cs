using System.Text.Json;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Import;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for CSV import operations including template management and data import execution.
/// </summary>
[ApiController]
[Route("api/v1/import")]
public class ImportController : ControllerBase
{
    private readonly IDataImportService _importService;
    private readonly ICsvParserService _csvParser;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IDataImportService importService,
        ICsvParserService csvParser,
        ILogger<ImportController> logger)
    {
        _importService = importService;
        _csvParser = csvParser;
        _logger = logger;
    }

    /// <summary>
    /// Upload a CSV file and generate a preview with headers and sample rows.
    /// </summary>
    /// <param name="file">The CSV file to preview</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>CSV preview with headers, sample rows, and metadata</returns>
    /// <response code="200">Returns CSV preview</response>
    /// <response code="400">File is missing or invalid</response>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(10_485_760)] // 10MB limit
    [ProducesResponseType(typeof(CsvPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPreview(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        try
        {
            await using var fileStream = file.OpenReadStream();
            var preview = await _csvParser.GeneratePreviewAsync(fileStream, ct);
            return Ok(new { data = preview });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CSV preview for file: {FileName}", file.FileName);
            return BadRequest(new { error = "Failed to parse CSV file" });
        }
    }

    /// <summary>
    /// Get all import templates, optionally filtered by import type.
    /// </summary>
    /// <param name="type">Optional import type filter (people, attendance, giving)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of import templates</returns>
    /// <response code="200">Returns list of templates</response>
    /// <response code="400">Invalid import type specified</response>
    [HttpGet("templates")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ImportTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? type,
        CancellationToken ct = default)
    {
        ImportType? importType = null;
        if (!string.IsNullOrEmpty(type))
        {
            if (!Enum.TryParse<ImportType>(type, true, out var parsed))
            {
                return BadRequest(new { error = "Invalid import type. Must be one of: People, Attendance, Giving" });
            }
            importType = parsed;
        }

        IReadOnlyList<ImportTemplateDto> templates = importType.HasValue
            ? await _importService.GetTemplatesAsync(importType.Value, ct)
            : await _importService.GetAllTemplatesAsync(ct);

        return Ok(new { data = templates });
    }

    /// <summary>
    /// Create a new import template with field mappings.
    /// </summary>
    /// <param name="request">Template creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created template</returns>
    /// <response code="201">Template created successfully</response>
    /// <response code="400">Invalid request or validation failure</response>
    [HttpPost("templates")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ImportTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateImportTemplateRequest request,
        CancellationToken ct = default)
    {
        var result = await _importService.CreateTemplateAsync(request, ct);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetTemplate),
            new { idKey = result.Value!.IdKey },
            new { data = result.Value });
    }

    /// <summary>
    /// Get a specific import template by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded template ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Template details</returns>
    /// <response code="200">Returns template details</response>
    /// <response code="404">Template not found</response>
    [HttpGet("templates/{idKey}")]
    [Authorize]
    [ProducesResponseType(typeof(ImportTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(
        string idKey,
        CancellationToken ct = default)
    {
        var result = await _importService.GetTemplateAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(new { data = result.Value! });
    }

    /// <summary>
    /// Delete an import template by IdKey.
    /// </summary>
    /// <param name="idKey">Encoded template ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Template deleted successfully</response>
    /// <response code="404">Template not found</response>
    [HttpDelete("templates/{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(
        string idKey,
        CancellationToken ct = default)
    {
        var result = await _importService.DeleteTemplateAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Validate field mappings against a CSV file before import.
    /// </summary>
    /// <param name="file">CSV file to validate</param>
    /// <param name="importType">Import type (people, attendance, giving)</param>
    /// <param name="fieldMappingsJson">JSON string of field mappings (CSV column -> entity field)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Import job with validation results</returns>
    /// <response code="200">Returns validation results</response>
    /// <response code="400">Invalid request or validation failure</response>
    [HttpPost("validate")]
    [Authorize]
    [RequestSizeLimit(10_485_760)] // 10MB limit
    [ProducesResponseType(typeof(ImportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateImport(
        IFormFile file,
        [FromForm] string importType,
        [FromForm] string fieldMappingsJson,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        if (string.IsNullOrEmpty(fieldMappingsJson))
        {
            return BadRequest(new { error = "Field mappings are required" });
        }

        // Parse field mappings JSON
        Dictionary<string, string> mappings;
        try
        {
            mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(fieldMappingsJson)
                ?? new Dictionary<string, string>();
            
            if (mappings.Count == 0)
            {
                return BadRequest(new { error = "Field mappings must be a valid JSON object" });
            }
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Invalid field mappings JSON format" });
        }

        try
        {
            await using var fileStream = file.OpenReadStream();
            var request = new ValidateImportRequest
            {
                FileStream = fileStream,
                FileName = file.FileName,
                ImportType = importType,
                FieldMappings = mappings
            };

            var result = await _importService.ValidateMappingsAsync(request, ct);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { data = result.Value! });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate import for file: {FileName}", file.FileName);
            return BadRequest(new { error = "Failed to validate import" });
        }
    }

    /// <summary>
    /// Start an import job to process CSV data.
    /// </summary>
    /// <param name="file">CSV file to import</param>
    /// <param name="importType">Import type (people, attendance, giving)</param>
    /// <param name="fieldMappingsJson">JSON string of field mappings (CSV column -> entity field)</param>
    /// <param name="templateIdKey">Optional template IdKey to associate with import</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Started import job</returns>
    /// <response code="202">Import job started successfully</response>
    /// <response code="400">Invalid request or validation failure</response>
    [HttpPost("execute")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_485_760)] // 10MB limit
    [ProducesResponseType(typeof(ImportJobDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteImport(
        IFormFile file,
        [FromForm] string importType,
        [FromForm] string fieldMappingsJson,
        [FromForm] string? templateIdKey = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        if (string.IsNullOrEmpty(fieldMappingsJson))
        {
            return BadRequest(new { error = "Field mappings are required" });
        }

        // Parse field mappings JSON
        Dictionary<string, string> mappings;
        try
        {
            mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(fieldMappingsJson)
                ?? new Dictionary<string, string>();
            
            if (mappings.Count == 0)
            {
                return BadRequest(new { error = "Field mappings must be a valid JSON object" });
            }
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Invalid field mappings JSON format" });
        }

        try
        {
            await using var fileStream = file.OpenReadStream();
            var request = new StartImportRequest
            {
                FileStream = fileStream,
                FileName = file.FileName,
                ImportType = importType,
                FieldMappings = mappings,
                ImportTemplateIdKey = templateIdKey
            };

            var result = await _importService.StartImportAsync(request, ct);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return AcceptedAtAction(
                nameof(GetJobStatus),
                new { idKey = result.Value!.IdKey },
                new { data = result.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start import for file: {FileName}", file.FileName);
            return BadRequest(new { error = "Failed to start import" });
        }
    }

    /// <summary>
    /// Get the status and progress of an import job.
    /// </summary>
    /// <param name="idKey">Encoded job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Job status and progress</returns>
    /// <response code="200">Returns job status</response>
    /// <response code="404">Job not found</response>
    [HttpGet("jobs/{idKey}")]
    [Authorize]
    [ProducesResponseType(typeof(ImportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(
        string idKey,
        CancellationToken ct = default)
    {
        var result = await _importService.GetImportStatusAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(new { data = result.Value! });
    }

    /// <summary>
    /// Download CSV error report for a completed import job.
    /// </summary>
    /// <param name="idKey">Encoded job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>CSV file containing error details</returns>
    /// <response code="200">Returns CSV error report</response>
    /// <response code="404">Job not found or has no errors</response>
    [HttpGet("jobs/{idKey}/errors")]
    [Authorize]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadErrors(
        string idKey,
        CancellationToken ct = default)
    {
        var result = await _importService.GenerateErrorReportAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        var fileName = $"import-{idKey}-errors.csv";
        return File(result.Value!, "text/csv", fileName);
    }
}
