using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// API controller for managing in-app notifications.
/// Provides endpoints for retrieving, marking as read, and managing notification preferences.
/// All endpoints operate on the currently authenticated user's notifications.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController(
    INotificationService notificationService,
    INotificationHubContext notificationHubContext,
    IUserContext userContext,
    ILogger<NotificationsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets notifications for the current user.
    /// </summary>
    /// <param name="unreadOnly">If true, returns only unread notifications. If false, returns only read notifications. If null, returns all.</param>
    /// <param name="limit">Maximum number of notifications to return (default: 50, max: 100).</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of notifications ordered by creation date (newest first)</returns>
    /// <response code="200">Returns list of notifications</response>
    /// <response code="400">Invalid limit parameter</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int? limit = 50,
        CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to access notifications",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        // Validate limit parameter
        if (limit.HasValue && (limit.Value < 1 || limit.Value > 100))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid limit parameter",
                Detail = "Limit must be between 1 and 100",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var notifications = await notificationService.GetForPersonAsync(
            personIdKey,
            unreadOnly,
            limit,
            ct);

        logger.LogDebug(
            "Retrieved {Count} notifications: PersonIdKey={PersonIdKey}, UnreadOnly={UnreadOnly}, Limit={Limit}",
            notifications.Count, personIdKey, unreadOnly, limit);

        return Ok(new { data = notifications });
    }

    /// <summary>
    /// Gets the count of unread notifications for the current user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unread notification count</returns>
    /// <response code="200">Returns unread count</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to access notifications",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var count = await notificationService.GetUnreadCountAsync(personIdKey, ct);

        logger.LogDebug("Unread count retrieved: PersonIdKey={PersonIdKey}, Count={Count}", personIdKey, count);

        return Ok(new { count });
    }

    /// <summary>
    /// Gets a single notification by its IdKey.
    /// </summary>
    /// <param name="idKey">The notification's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Notification details</returns>
    /// <response code="200">Returns notification details</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Notification not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotification(string idKey, CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to access notifications",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var notification = await notificationService.GetByIdKeyAsync(idKey, ct);

        if (notification == null)
        {
            logger.LogDebug("Notification not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Notification not found",
                Detail = $"No notification found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Notification retrieved: IdKey={IdKey}", idKey);

        return Ok(new { data = notification });
    }

    /// <summary>
    /// Marks a notification as read.
    /// After marking, sends updated unread count via SignalR.
    /// </summary>
    /// <param name="idKey">The notification's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Notification marked as read</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Notification not found</response>
    [HttpPut("{idKey}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(string idKey, CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to mark notifications as read",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var success = await notificationService.MarkAsReadAsync(idKey, personIdKey, ct);

        if (!success)
        {
            logger.LogDebug("Notification not found or access denied: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Notification not found",
                Detail = $"No notification found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        // Send updated unread count via SignalR
        var unreadCount = await notificationService.GetUnreadCountAsync(personIdKey, ct);
        await notificationHubContext.SendUnreadCountAsync(personIdKey, unreadCount, ct);

        logger.LogInformation(
            "Notification marked as read: IdKey={IdKey}, PersonIdKey={PersonIdKey}, NewUnreadCount={UnreadCount}",
            idKey, personIdKey, unreadCount);

        return NoContent();
    }

    /// <summary>
    /// Marks all notifications as read for the current user.
    /// After marking, sends updated count (0) via SignalR.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of notifications marked as read</returns>
    /// <response code="200">Returns count of notifications marked as read</response>
    /// <response code="401">User not authenticated</response>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to mark notifications as read",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var markedCount = await notificationService.MarkAllAsReadAsync(personIdKey, ct);

        // Send updated unread count (0) via SignalR
        await notificationHubContext.SendUnreadCountAsync(personIdKey, 0, ct);

        logger.LogInformation(
            "All notifications marked as read: PersonIdKey={PersonIdKey}, MarkedCount={MarkedCount}",
            personIdKey, markedCount);

        return Ok(new { markedCount });
    }

    /// <summary>
    /// Deletes a notification.
    /// After deleting, sends updated unread count via SignalR if the deleted notification was unread.
    /// </summary>
    /// <param name="idKey">The notification's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Notification deleted</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Notification not found</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(string idKey, CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to delete notifications",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        // Get the notification before deleting to check if it was unread
        var notification = await notificationService.GetByIdKeyAsync(idKey, ct);
        if (notification == null)
        {
            logger.LogDebug("Notification not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Notification not found",
                Detail = $"No notification found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var wasUnread = !notification.IsRead;
        var success = await notificationService.DeleteAsync(idKey, personIdKey, ct);

        if (!success)
        {
            // This shouldn't happen since we just checked above, but handle it anyway
            logger.LogDebug("Notification delete failed (access denied): IdKey={IdKey}", idKey);
            return NotFound(new ProblemDetails
            {
                Title = "Notification not found",
                Detail = $"No notification found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        // If the deleted notification was unread, send updated unread count via SignalR
        if (wasUnread)
        {
            var unreadCount = await notificationService.GetUnreadCountAsync(personIdKey, ct);
            await notificationHubContext.SendUnreadCountAsync(personIdKey, unreadCount, ct);
        }

        logger.LogInformation(
            "Notification deleted: IdKey={IdKey}, PersonIdKey={PersonIdKey}, WasUnread={WasUnread}",
            idKey, personIdKey, wasUnread);

        return NoContent();
    }

    /// <summary>
    /// Gets notification preferences for the current user.
    /// Returns preferences for all notification types, using defaults if not explicitly set.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of notification preferences</returns>
    /// <response code="200">Returns notification preferences</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to access notification preferences",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var preferences = await notificationService.GetPreferencesAsync(personIdKey, ct);

        logger.LogDebug(
            "Notification preferences retrieved: PersonIdKey={PersonIdKey}, Count={Count}",
            personIdKey, preferences.Count);

        return Ok(new { data = preferences });
    }

    /// <summary>
    /// Updates a notification preference for the current user.
    /// Creates the preference if it doesn't exist.
    /// </summary>
    /// <param name="request">The preference update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated preference</returns>
    /// <response code="200">Preference updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">User not authenticated</response>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(NotificationPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreference(
        [FromBody] UpdateNotificationPreferenceDto request,
        CancellationToken ct = default)
    {
        var personIdKey = GetCurrentPersonIdKey();
        if (personIdKey == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to update notification preferences",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var preference = await notificationService.UpdatePreferenceAsync(personIdKey, request, ct);

        logger.LogInformation(
            "Notification preference updated: PersonIdKey={PersonIdKey}, Type={Type}, Enabled={Enabled}",
            personIdKey, request.NotificationType, request.IsEnabled);

        return Ok(new { data = preference });
    }

    /// <summary>
    /// Gets the current user's PersonIdKey from the IUserContext.
    /// </summary>
    /// <returns>The PersonIdKey if authenticated, null otherwise.</returns>
    private string? GetCurrentPersonIdKey()
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            return null;
        }

        return IdKeyHelper.Encode(userContext.CurrentPersonId.Value);
    }
}
