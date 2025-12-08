namespace Koinon.Domain.Enums;

/// <summary>
/// Status of a follow-up task for a visitor or attendee.
/// </summary>
public enum FollowUpStatus
{
    /// <summary>
    /// Follow-up is pending and has not yet been attempted.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Contact has been made with the person.
    /// </summary>
    Contacted = 1,

    /// <summary>
    /// Contact was attempted but no response was received.
    /// </summary>
    NoResponse = 2,

    /// <summary>
    /// Successfully connected with the person and follow-up is complete.
    /// </summary>
    Connected = 3,

    /// <summary>
    /// Person declined further contact or follow-up.
    /// </summary>
    Declined = 4
}
