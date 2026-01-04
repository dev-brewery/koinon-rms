using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// Response DTO for user notification preferences.
/// </summary>
public record NotificationPreferenceDto
{
    /// <summary>
    /// Encoded identifier for the preference.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// The notification type this preference applies to.
    /// </summary>
    public NotificationType NotificationType { get; init; }

    /// <summary>
    /// Whether this notification type is enabled for the user.
    /// </summary>
    public bool IsEnabled { get; init; }
}
