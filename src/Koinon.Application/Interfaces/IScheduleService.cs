using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for schedule management operations.
/// Handles service times, check-in windows, and schedule occurrences.
/// </summary>
public interface IScheduleService
{
    /// <summary>
    /// Gets a schedule by its integer ID.
    /// </summary>
    Task<ScheduleDto?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets a schedule by its IdKey.
    /// </summary>
    Task<ScheduleDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Searches for schedules with pagination and filtering.
    /// </summary>
    Task<PagedResult<ScheduleSummaryDto>> SearchAsync(
        ScheduleSearchParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new schedule.
    /// </summary>
    Task<Result<ScheduleDto>> CreateAsync(
        CreateScheduleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing schedule.
    /// </summary>
    Task<Result<ScheduleDto>> UpdateAsync(
        string idKey,
        UpdateScheduleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deactivates a schedule (soft delete).
    /// </summary>
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Gets upcoming occurrences for a schedule.
    /// </summary>
    /// <param name="idKey">Schedule IdKey</param>
    /// <param name="startDate">Start date for occurrence calculation (defaults to today)</param>
    /// <param name="count">Number of occurrences to return (default 10, max 52)</param>
    /// <param name="ct">Cancellation token</param>
    Task<IReadOnlyList<ScheduleOccurrenceDto>> GetOccurrencesAsync(
        string idKey,
        DateOnly? startDate = null,
        int count = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the next occurrence for a schedule.
    /// </summary>
    /// <param name="idKey">Schedule IdKey</param>
    /// <param name="fromDate">Calculate next occurrence from this date (defaults to now)</param>
    /// <param name="ct">Cancellation token</param>
    Task<ScheduleOccurrenceDto?> GetNextOccurrenceAsync(
        string idKey,
        DateTime? fromDate = null,
        CancellationToken ct = default);
}
