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
    Task<ImportJobDto?> GetImportStatusAsync(string jobIdKey, CancellationToken ct = default);
    
    // Error handling
    
    /// <summary>
    /// Generates a CSV error report for a completed import job.
    /// </summary>
    Task<Result<Stream>> GenerateErrorReportAsync(string jobIdKey, CancellationToken ct = default);
}
