using Koinon.Application.Common;
using Koinon.Application.DTOs.Reports;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for report definition and execution operations.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Gets all report definitions with optional filtering.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive report definitions</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of report definitions</returns>
    Task<PagedResult<ReportDefinitionDto>> GetDefinitionsAsync(
        bool includeInactive = false,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a report definition by IdKey.
    /// </summary>
    /// <param name="idKey">Report definition IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report definition DTO or null if not found</returns>
    Task<ReportDefinitionDto?> GetDefinitionAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new report definition.
    /// </summary>
    /// <param name="request">Create request with report configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with created report definition</returns>
    Task<Result<ReportDefinitionDto>> CreateDefinitionAsync(
        CreateReportDefinitionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing report definition.
    /// </summary>
    /// <param name="idKey">Report definition IdKey</param>
    /// <param name="request">Update request with changes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with updated report definition</returns>
    Task<Result<ReportDefinitionDto>> UpdateDefinitionAsync(
        string idKey,
        UpdateReportDefinitionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a report definition (soft delete).
    /// </summary>
    /// <param name="idKey">Report definition IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DeleteDefinitionAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Runs a report and queues it for generation.
    /// </summary>
    /// <param name="request">Run request with report parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with report run DTO containing execution status</returns>
    Task<Result<ReportRunDto>> RunReportAsync(
        RunReportRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all report runs with optional filtering.
    /// </summary>
    /// <param name="reportDefinitionIdKey">Optional filter by report definition</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of report runs</returns>
    Task<PagedResult<ReportRunDto>> GetRunsAsync(
        string? reportDefinitionIdKey = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific report run by IdKey.
    /// </summary>
    /// <param name="idKey">Report run IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report run DTO or null if not found</returns>
    Task<ReportRunDto?> GetRunAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a generated report file.
    /// </summary>
    /// <param name="reportRunIdKey">Report run IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (stream, fileName, mimeType) or null if not found</returns>
    Task<(Stream Stream, string FileName, string MimeType)?> DownloadReportAsync(
        string reportRunIdKey,
        CancellationToken ct = default);
}
