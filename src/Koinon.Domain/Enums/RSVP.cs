namespace Koinon.Domain.Enums;

/// <summary>
/// RSVP status for attendance.
/// </summary>
public enum RSVP
{
    /// <summary>
    /// Person declined to attend.
    /// </summary>
    No = 0,

    /// <summary>
    /// Person confirmed they will attend.
    /// </summary>
    Yes = 1,

    /// <summary>
    /// Person indicated they might attend.
    /// </summary>
    Maybe = 2,

    /// <summary>
    /// RSVP status is unknown.
    /// </summary>
    Unknown = 3
}
