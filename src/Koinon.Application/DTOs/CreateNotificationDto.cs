using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// DTO for creating new in-app notifications (internal use).
/// </summary>
public record CreateNotificationDto
{
    /// <summary>
    /// Encoded identifier of the person who will receive this notification.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// The type of notification.
    /// </summary>
    public NotificationType NotificationType { get; init; }

    /// <summary>
    /// Short title for the notification.
    /// Maximum length: 200 characters.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Full message content of the notification.
    /// Maximum length: 1000 characters.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional URL to navigate to when the notification is clicked.
    /// Maximum length: 500 characters.
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Optional JSON metadata for additional context.
    /// </summary>
    public string? MetadataJson { get; init; }
}
