using Koinon.Application.Common;
using Koinon.Application.DTOs.VolunteerSchedule;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for managing volunteer schedule assignments.
/// </summary>
public interface IVolunteerScheduleService
{
    /// <summary>
    /// Creates multiple schedule assignments for volunteers.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey.</param>
    /// <param name="request">Assignment creation request containing members, schedule, and dates.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of created assignments.</returns>
    Task<Result<List<ScheduleAssignmentDto>>> CreateAssignmentsAsync(
        string groupIdKey,
        CreateScheduleAssignmentsRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all assignments for a group within a date range.
    /// </summary>
    /// <param name="groupIdKey">The group's IdKey.</param>
    /// <param name="startDate">Start of date range (inclusive).</param>
    /// <param name="endDate">End of date range (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of assignments in the specified range.</returns>
    Task<List<ScheduleAssignmentDto>> GetAssignmentsAsync(
        string groupIdKey,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a volunteer assignment (confirm/decline).
    /// </summary>
    /// <param name="assignmentIdKey">The assignment's IdKey.</param>
    /// <param name="request">Status update request.</param>
    /// <param name="currentPersonId">The ID of the person making the update (for ownership check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated assignment.</returns>
    Task<Result<ScheduleAssignmentDto>> UpdateAssignmentStatusAsync(
        string assignmentIdKey,
        UpdateAssignmentStatusRequest request,
        int currentPersonId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current user's upcoming schedule assignments.
    /// </summary>
    /// <param name="personId">The person's ID.</param>
    /// <param name="startDate">Optional start date (defaults to today).</param>
    /// <param name="endDate">Optional end date (defaults to 90 days from start).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of assignments grouped by date.</returns>
    Task<List<MyScheduleDto>> GetMyScheduleAsync(
        int personId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken ct = default);
}
