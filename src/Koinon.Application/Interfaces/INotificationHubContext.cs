using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Interface for sending real-time notifications via SignalR.
/// Abstracts SignalR hub context to allow Application layer to send notifications
/// without depending on SignalR directly.
/// </summary>
public interface INotificationHubContext
{
    /// <summary>
    /// Sends a notification to a specific person via SignalR.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey to send the notification to.</param>
    /// <param name="notification">The notification data to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendNotificationAsync(string personIdKey, NotificationDto notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an updated unread count to a specific person via SignalR.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey to send the count to.</param>
    /// <param name="count">The number of unread notifications.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendUnreadCountAsync(string personIdKey, int count, CancellationToken cancellationToken = default);
}
