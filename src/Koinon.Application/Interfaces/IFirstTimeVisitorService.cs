using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for first-time visitor tracking and management.
/// </summary>
public interface IFirstTimeVisitorService
{
    /// <summary>
    /// Gets all first-time visitors who checked in today.
    /// </summary>
    /// <param name="campusIdKey">Optional campus filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of first-time visitors.</returns>
    Task<IReadOnlyList<FirstTimeVisitorDto>> GetTodaysFirstTimersAsync(
        string? campusIdKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets first-time visitors who checked in within a date range.
    /// </summary>
    /// <param name="startDate">Start date of the range.</param>
    /// <param name="endDate">End date of the range.</param>
    /// <param name="campusIdKey">Optional campus filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of first-time visitors.</returns>
    Task<IReadOnlyList<FirstTimeVisitorDto>> GetFirstTimersByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? campusIdKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a person is checking in for the first time at a specific group type.
    /// </summary>
    /// <param name="personId">The person's ID.</param>
    /// <param name="groupTypeId">The group type ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if this is the person's first time at this group type.</returns>
    Task<bool> IsFirstTimeForGroupTypeAsync(
        int personId,
        int groupTypeId,
        CancellationToken ct = default);
}
