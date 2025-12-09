using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs.VolunteerSchedule;

/// <summary>
/// Request to update a volunteer assignment status (confirm/decline).
/// </summary>
public record UpdateAssignmentStatusRequest
{
    /// <summary>
    /// New status for the assignment.
    /// </summary>
    public required VolunteerScheduleStatus Status { get; init; }

    /// <summary>
    /// Optional reason for declining (required if Status = Declined).
    /// </summary>
    public string? DeclineReason { get; init; }
}
