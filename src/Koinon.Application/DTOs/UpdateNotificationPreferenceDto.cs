using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for updating user notification preferences.
/// </summary>
public record UpdateNotificationPreferenceDto
{
    /// <summary>
    /// The notification type to update.
    /// </summary>
    public NotificationType NotificationType { get; init; }

    /// <summary>
    /// Whether this notification type should be enabled for the user.
    /// </summary>
    public bool IsEnabled { get; init; }
}
