namespace Koinon.Domain.Enums;

/// <summary>
/// Types of in-app notifications
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Alerts about check-in events (e.g., child checked in, security code used)
    /// </summary>
    CheckinAlert = 0,

    /// <summary>
    /// Communication delivery status updates (e.g., email bounced, SMS failed)
    /// </summary>
    CommunicationStatus = 1,

    /// <summary>
    /// System-wide announcements and alerts
    /// </summary>
    SystemAlert = 2,

    /// <summary>
    /// Group membership request notifications
    /// </summary>
    MembershipRequest = 3,

    /// <summary>
    /// Follow-up task reminders
    /// </summary>
    FollowUp = 4
}
