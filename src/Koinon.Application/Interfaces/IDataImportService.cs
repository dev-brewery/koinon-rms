using Koinon.Application.Common;
using Koinon.Application.DTOs.Import;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing data import templates and executing CSV imports.
/// </summary>
public interface IDataImportService
{
    // Template management

    /// <summary>
    /// Creates a new import template with field mappings.
    /// </summary>
    Task<Result<ImportTemplateDto>> CreateTemplateAsync(CreateImportTemplateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets all templates for a specific import type.
    /// </summary>
    Task<IReadOnlyList<ImportTemplateDto>> GetTemplatesAsync(ImportType type, CancellationToken ct = default);

    /// <summary>
    /// Gets all templates across all import types.
    /// </summary>
    Task<IReadOnlyList<ImportTemplateDto>> GetAllTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a single template by IdKey.
    /// </summary>
    Task<Result<ImportTemplateDto>> GetTemplateAsync(string templateIdKey, CancellationToken ct = default);

    /// <summary>
    /// Deletes (soft delete) a template by IdKey.
    /// </summary>
    Task<Result> DeleteTemplateAsync(string templateIdKey, CancellationToken ct = default);

    // Import execution

    /// <summary>
    /// Validates field mappings against CSV file and returns validation results.
    /// Creates a pending import job with validation status.
    /// </summary>
    Task<Result<ImportJobDto>> ValidateMappingsAsync(ValidateImportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Starts an import job, processing CSV file in batches.
    /// </summary>
    Task<Result<ImportJobDto>> StartImportAsync(StartImportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the current status of an import job.
    /// </summary>
    Task<Result<ImportJobDto>> GetImportStatusAsync(string jobIdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets a paginated list of import jobs, optionally filtered by import type.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="importType">Optional filter by import type</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of import jobs</returns>
    Task<PagedResult<ImportJobDto>> GetImportJobsAsync(int page, int pageSize, ImportType? importType = null, CancellationToken ct = default);

    // Error handling

    /// <summary>
    /// Generates a CSV error report for a completed import job.
    /// </summary>
    Task<Result<Stream>> GenerateErrorReportAsync(string jobIdKey, CancellationToken ct = default);

    // Background job processing

    /// <summary>
    /// Processes an import job in the background (called by Hangfire).
    /// </summary>
    /// <param name="jobIdKey">Import job IdKey</param>
    /// <param name="fieldMappingsJson">JSON-serialized field mappings</param>
    /// <param name="ct">Cancellation token</param>
    Task ProcessImportJobAsync(string jobIdKey, string fieldMappingsJson, CancellationToken ct = default);
}
