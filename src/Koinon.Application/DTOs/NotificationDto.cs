using Koinon.Domain.Enums;

namespace Koinon.Application.DTOs;

/// <summary>
/// Response DTO for in-app notifications.
/// </summary>
public record NotificationDto
{
    /// <summary>
    /// Encoded identifier for the notification.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Unique identifier for the notification.
    /// </summary>
    public required Guid Guid { get; init; }

    /// <summary>
    /// The type of notification.
    /// </summary>
    public NotificationType NotificationType { get; init; }

    /// <summary>
    /// Short title for the notification.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Full message content of the notification.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Whether the user has read this notification.
    /// </summary>
    public bool IsRead { get; init; }

    /// <summary>
    /// When the notification was marked as read.
    /// Null if not yet read.
    /// </summary>
    public DateTime? ReadDateTime { get; init; }

    /// <summary>
    /// Optional URL to navigate to when the notification is clicked.
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Optional JSON metadata for additional context.
    /// </summary>
    public string? MetadataJson { get; init; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime CreatedDateTime { get; init; }
}
