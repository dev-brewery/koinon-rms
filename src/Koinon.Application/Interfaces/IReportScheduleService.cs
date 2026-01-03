using Koinon.Application.Common;
using Koinon.Application.DTOs.Reports;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for scheduled report operations.
/// </summary>
public interface IReportScheduleService
{
    /// <summary>
    /// Gets all report schedules with optional filtering.
    /// </summary>
    /// <param name="reportDefinitionIdKey">Optional filter by report definition</param>
    /// <param name="includeInactive">Whether to include inactive schedules</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of report schedules</returns>
    Task<PagedResult<ReportScheduleDto>> GetSchedulesAsync(
        string? reportDefinitionIdKey = null,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a report schedule by IdKey.
    /// </summary>
    /// <param name="idKey">Report schedule IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report schedule DTO or null if not found</returns>
    Task<ReportScheduleDto?> GetScheduleAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new report schedule.
    /// </summary>
    /// <param name="request">Create request with schedule configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with created report schedule</returns>
    Task<Result<ReportScheduleDto>> CreateScheduleAsync(
        CreateReportScheduleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing report schedule.
    /// </summary>
    /// <param name="idKey">Report schedule IdKey</param>
    /// <param name="request">Update request with changes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with updated report schedule</returns>
    Task<Result<ReportScheduleDto>> UpdateScheduleAsync(
        string idKey,
        UpdateReportScheduleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a report schedule.
    /// </summary>
    /// <param name="idKey">Report schedule IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DeleteScheduleAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Manually triggers a scheduled report to run immediately.
    /// </summary>
    /// <param name="idKey">Report schedule IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with report run DTO containing execution status</returns>
    Task<Result<ReportRunDto>> TriggerScheduledReportAsync(
        string idKey,
        CancellationToken ct = default);
}
