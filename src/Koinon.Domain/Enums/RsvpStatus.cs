namespace Koinon.Domain.Enums;

/// <summary>
/// RSVP response status for group meeting attendance.
/// </summary>
public enum RsvpStatus
{
    /// <summary>
    /// No response received yet.
    /// </summary>
    NoResponse = 0,

    /// <summary>
    /// Person confirmed they will attend.
    /// </summary>
    Attending = 1,

    /// <summary>
    /// Person confirmed they will not attend.
    /// </summary>
    NotAttending = 2,

    /// <summary>
    /// Person indicated they might attend.
    /// </summary>
    Maybe = 3
}
