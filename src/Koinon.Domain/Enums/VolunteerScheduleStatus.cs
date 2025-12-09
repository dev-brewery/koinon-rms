namespace Koinon.Domain.Enums;

/// <summary>
/// Represents the status of a volunteer's schedule assignment response.
/// </summary>
public enum VolunteerScheduleStatus
{
    /// <summary>
    /// The volunteer has been scheduled but hasn't responded yet.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// The volunteer has confirmed they will serve.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// The volunteer has declined the assignment.
    /// </summary>
    Declined = 2,

    /// <summary>
    /// The volunteer hasn't responded after the deadline.
    /// </summary>
    NoResponse = 3
}
