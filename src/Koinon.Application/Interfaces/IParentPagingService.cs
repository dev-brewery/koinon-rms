using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for managing parent paging/notification system during Sunday morning check-in.
/// Allows staff to send SMS notifications to parents when their child needs attention.
/// </summary>
public interface IParentPagingService
{
    /// <summary>
    /// Assigns a pager number to an attendance record during check-in.
    /// Called automatically when a child is checked in.
    /// </summary>
    /// <param name="attendanceId">The attendance record ID</param>
    /// <param name="campusId">Optional campus ID for pager number scoping</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Pager assignment DTO with all related information</returns>
    Task<PagerAssignmentDto> AssignPagerAsync(int attendanceId, int? campusId, CancellationToken ct = default);

    /// <summary>
    /// Sends a page to a parent. Enforces rate limiting (max 3 per attendance per hour).
    /// </summary>
    /// <param name="request">Request containing pager number, message type, and optional custom message</param>
    /// <param name="sentByPersonId">The ID of the staff member sending the page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result containing the pager message DTO or error details</returns>
    Task<Result<PagerMessageDto>> SendPageAsync(SendPageRequest request, int sentByPersonId, CancellationToken ct = default);

    /// <summary>
    /// Searches for pager assignments (for supervisor lookup).
    /// </summary>
    /// <param name="request">Search criteria including search term, campus, and date</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching pager assignments</returns>
    Task<List<PagerAssignmentDto>> SearchPagerAsync(PageSearchRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets page history for a specific pager.
    /// </summary>
    /// <param name="pagerNumber">The numeric pager number (e.g., 127)</param>
    /// <param name="date">Optional date to query (defaults to today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Page history DTO with all messages, or null if not found</returns>
    Task<PageHistoryDto?> GetPageHistoryAsync(int pagerNumber, DateTime? date, CancellationToken ct = default);

    /// <summary>
    /// Gets the next available pager number for a campus/date.
    /// </summary>
    /// <param name="campusId">Optional campus ID for scoping</param>
    /// <param name="date">The date to check for (typically today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Next available pager number (starting at 100 for a new day)</returns>
    Task<int> GetNextPagerNumberAsync(int? campusId, DateTime date, CancellationToken ct = default);
}
