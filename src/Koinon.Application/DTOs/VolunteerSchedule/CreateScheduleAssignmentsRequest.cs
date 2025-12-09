namespace Koinon.Application.DTOs.VolunteerSchedule;

/// <summary>
/// Request to create volunteer schedule assignments.
/// </summary>
public record CreateScheduleAssignmentsRequest
{
    /// <summary>
    /// Array of member IdKeys to assign.
    /// </summary>
    public required string[] MemberIdKeys { get; init; }

    /// <summary>
    /// IdKey of the schedule to assign to.
    /// </summary>
    public required string ScheduleIdKey { get; init; }

    /// <summary>
    /// Array of dates to assign the members.
    /// </summary>
    public required DateOnly[] Dates { get; init; }
}
