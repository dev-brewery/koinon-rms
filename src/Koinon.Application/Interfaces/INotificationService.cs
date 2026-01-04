using Koinon.Application.DTOs;
using Koinon.Domain.Enums;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for managing in-app notifications and user preferences.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets a notification by its IdKey.
    /// </summary>
    /// <param name="idKey">The notification's IdKey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification DTO if found, null otherwise.</returns>
    Task<NotificationDto?> GetByIdKeyAsync(string idKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a specific person.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="unreadOnly">If true, returns only unread notifications. If false, returns only read notifications. If null, returns all.</param>
    /// <param name="limit">Maximum number of notifications to return. If null, returns all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of notifications ordered by creation date (newest first).</returns>
    Task<IReadOnlyList<NotificationDto>> GetForPersonAsync(
        string personIdKey,
        bool? unreadOnly = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a person.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of unread notifications.</returns>
    Task<int> GetUnreadCountAsync(string personIdKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    /// <param name="dto">The notification data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created notification DTO.</returns>
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="idKey">The notification's IdKey.</param>
    /// <param name="personIdKey">The person's IdKey (for ownership validation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the notification was marked as read, false if not found or not owned by person.</returns>
    Task<bool> MarkAsReadAsync(string idKey, string personIdKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications for a person as read.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of notifications that were marked as read.</returns>
    Task<int> MarkAllAsReadAsync(string personIdKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification.
    /// </summary>
    /// <param name="idKey">The notification's IdKey.</param>
    /// <param name="personIdKey">The person's IdKey (for ownership validation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the notification was deleted, false if not found or not owned by person.</returns>
    Task<bool> DeleteAsync(string idKey, string personIdKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification preferences for a person.
    /// Returns preferences for all notification types, using defaults if not explicitly set.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of notification preferences for all notification types.</returns>
    Task<IReadOnlyList<NotificationPreferenceDto>> GetPreferencesAsync(
        string personIdKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a notification preference for a person.
    /// Creates the preference if it doesn't exist.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="dto">The preference update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated preference DTO.</returns>
    Task<NotificationPreferenceDto> UpdatePreferenceAsync(
        string personIdKey,
        UpdateNotificationPreferenceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a notification type is enabled for a person.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey.</param>
    /// <param name="type">The notification type to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the notification type is enabled (or no preference exists, defaulting to true).</returns>
    Task<bool> IsNotificationTypeEnabledAsync(
        string personIdKey,
        NotificationType type,
        CancellationToken cancellationToken = default);
}
