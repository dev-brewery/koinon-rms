using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for audit log operations.
/// Provides endpoints for searching, viewing, and exporting audit trail data.
/// Admin-only access for compliance and security purposes.
/// </summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "Admin")]
[ValidateIdKey]
public class AuditLogsController(
    IAuditService auditService,
    ILogger<AuditLogsController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for audit log entries with optional filters and pagination.
    /// </summary>
    /// <param name="startDate">Start of the date range to search (inclusive)</param>
    /// <param name="endDate">End of the date range to search (inclusive)</param>
    /// <param name="entityType">Filter by entity type (e.g., "Person", "Group")</param>
    /// <param name="actionType">Filter by action type (Create, Update, Delete, etc.)</param>
    /// <param name="personIdKey">Filter by person who performed the action</param>
    /// <param name="entityIdKey">Filter by specific entity that was affected</param>
    /// <param name="page">Page number for pagination (1-based, default: 1)</param>
    /// <param name="pageSize">Number of results per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of audit log entries</returns>
    /// <response code="200">Returns paginated list of audit log entries</response>
    /// <response code="400">Invalid IdKey format or parameters</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (requires Admin role)</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? entityType,
        [FromQuery] AuditAction? actionType,
        [FromQuery] string? personIdKey,
        [FromQuery] string? entityIdKey,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var parameters = new AuditLogSearchParameters
        {
            StartDate = startDate,
            EndDate = endDate,
            EntityType = entityType,
            ActionType = actionType,
            PersonIdKey = personIdKey,
            EntityIdKey = entityIdKey,
            Page = page,
            PageSize = Math.Min(pageSize, 100) // Enforce max page size
        };

        var result = await auditService.SearchAsync(parameters, ct);

        logger.LogInformation(
            "Audit log search completed: EntityType={EntityType}, ActionType={ActionType}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            entityType, actionType, result.Page, result.PageSize, result.TotalCount);

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
    /// Gets the complete audit history for a specific entity.
    /// Returns entries in chronological order (oldest to newest).
    /// </summary>
    /// <param name="entityType">Type of entity (e.g., "Person", "Group")</param>
    /// <param name="idKey">IdKey of the entity</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of audit log entries for the entity</returns>
    /// <response code="200">Returns audit history for the entity</response>
    /// <response code="400">Invalid IdKey format or entity type</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (requires Admin role)</response>
    [HttpGet("entity/{entityType}/{idKey}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByEntity(
        [FromRoute] string entityType,
        [FromRoute] string idKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid entity type",
                Detail = "Entity type cannot be empty.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var entries = await auditService.GetByEntityAsync(entityType, idKey, ct);

        logger.LogInformation(
            "Audit history retrieved: EntityType={EntityType}, IdKey={IdKey}, EntryCount={EntryCount}",
            entityType, idKey, entries.Count());

        return Ok(new { data = entries });
    }

    /// <summary>
    /// Exports audit log entries to a downloadable file.
    /// Supports various formats including CSV, JSON, and Excel.
    /// </summary>
    /// <param name="startDate">Start of the date range to export (inclusive)</param>
    /// <param name="endDate">End of the date range to export (inclusive)</param>
    /// <param name="entityType">Filter by entity type</param>
    /// <param name="actionType">Filter by action type</param>
    /// <param name="personIdKey">Filter by person who performed the action</param>
    /// <param name="format">Export format (Csv, Json, Excel - default: Csv)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download with audit log data</returns>
    /// <response code="200">Returns file download with audit log data</response>
    /// <response code="400">Invalid IdKey format or parameters</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (requires Admin role)</response>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? entityType,
        [FromQuery] AuditAction? actionType,
        [FromQuery] string? personIdKey,
        [FromQuery] ExportFormat format = ExportFormat.Csv,
        CancellationToken ct = default)
    {
        var request = new AuditLogExportRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            EntityType = entityType,
            ActionType = actionType,
            PersonIdKey = personIdKey,
            Format = format
        };

        var fileBytes = await auditService.ExportAsync(request, ct);

        // Determine file extension and content type based on format
        var (contentType, extension) = format switch
        {
            ExportFormat.Csv => ("text/csv", "csv"),
            ExportFormat.Json => ("application/json", "json"),
            ExportFormat.Excel => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            _ => ("text/csv", "csv")
        };

        var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{extension}";

        logger.LogInformation(
            "Audit log export completed: Format={Format}, EntityType={EntityType}, ActionType={ActionType}, FileSize={FileSize}",
            format, entityType, actionType, fileBytes.Length);

        return File(fileBytes, contentType, fileName);
    }
}
