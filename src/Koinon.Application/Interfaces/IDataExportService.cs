using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Exports;
using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing data export jobs and executing export operations.
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Gets a paginated list of export jobs.
    /// </summary>
    Task<PagedResult<ExportJobDto>> GetExportJobsAsync(
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific export job by IdKey.
    /// </summary>
    Task<ExportJobDto?> GetExportJobAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Starts a new data export job and queues it for processing.
    /// </summary>
    Task<Result<ExportJobDto>> StartExportAsync(
        StartExportRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Background job method to process export generation.
    /// Called by background job service after StartExportAsync enqueues the job.
    /// </summary>
    Task ProcessExportJobAsync(
        int exportJobId,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads the exported file for a completed export job.
    /// Returns null if the export is not complete or file not found.
    /// </summary>
    Task<(Stream Stream, string FileName, string MimeType)?> DownloadExportAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the list of available fields for a specific export type.
    /// </summary>
    List<ExportFieldDto> GetAvailableFields(ExportType exportType);
}
