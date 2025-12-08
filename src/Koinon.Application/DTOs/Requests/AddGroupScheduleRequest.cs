namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to add a schedule to a group.
/// </summary>
public record AddGroupScheduleRequest
{
    /// <summary>
    /// IdKey of the schedule to add.
    /// </summary>
    public required string ScheduleIdKey { get; init; }

    /// <summary>
    /// Display order for this schedule within the group.
    /// </summary>
    public int Order { get; init; }
}
