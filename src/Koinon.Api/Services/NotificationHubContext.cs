using Koinon.Api.Hubs;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Koinon.Api.Services;

/// <summary>
/// Implementation of INotificationHubContext that wraps SignalR hub context.
/// Provides a clean abstraction for the Application layer to send real-time notifications.
/// </summary>
public class NotificationHubContext(
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationHubContext> logger) : INotificationHubContext
{
    /// <summary>
    /// Sends a notification to a specific person via SignalR.
    /// The person must be connected to the NotificationHub to receive it.
    /// </summary>
    public async Task SendNotificationAsync(
        string personIdKey,
        NotificationDto notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients
                .Group(personIdKey)
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            logger.LogInformation(
                "Sent notification {NotificationId} to person {PersonIdKey}",
                notification.IdKey,
                personIdKey);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send notification {NotificationId} to person {PersonIdKey}",
                notification.IdKey,
                personIdKey);
            throw;
        }
    }

    /// <summary>
    /// Sends an updated unread count to a specific person via SignalR.
    /// The person must be connected to the NotificationHub to receive it.
    /// </summary>
    public async Task SendUnreadCountAsync(
        string personIdKey,
        int count,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients
                .Group(personIdKey)
                .SendAsync("UnreadCountUpdated", count, cancellationToken);

            logger.LogInformation(
                "Sent unread count {Count} to person {PersonIdKey}",
                count,
                personIdKey);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send unread count to person {PersonIdKey}",
                personIdKey);
            throw;
        }
    }
}
