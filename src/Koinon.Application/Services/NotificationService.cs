using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing in-app notifications and user notification preferences.
/// Handles notification creation, retrieval, read status, and user-configurable preferences.
/// </summary>
public class NotificationService(
    IApplicationDbContext context,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<NotificationDto?> GetByIdKeyAsync(
        string idKey,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            logger.LogWarning("Invalid notification IdKey: {IdKey}", idKey);
            return null;
        }

        var notification = await context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification == null)
        {
            return null;
        }

        return MapToDto(notification);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForPersonAsync(
        string personIdKey,
        bool? unreadOnly = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid person IdKey: {IdKey}", personIdKey);
            return Array.Empty<NotificationDto>();
        }

        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.PersonId == personId);

        // Apply read status filter
        if (unreadOnly.HasValue)
        {
            query = query.Where(n => n.IsRead == !unreadOnly.Value);
        }

        // Order by newest first
        query = query.OrderByDescending(n => n.CreatedDateTime);

        // Apply limit
        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        var notifications = await query.ToListAsync(cancellationToken);

        logger.LogInformation(
            "Retrieved {Count} notifications for person {PersonIdKey} (unreadOnly: {UnreadOnly}, limit: {Limit})",
            notifications.Count, personIdKey, unreadOnly, limit);

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(
        string personIdKey,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid person IdKey: {IdKey}", personIdKey);
            return 0;
        }

        var count = await context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.PersonId == personId && !n.IsRead, cancellationToken);

        return count;
    }

    public async Task<NotificationDto> CreateAsync(
        CreateNotificationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(dto.PersonIdKey, out int personId))
        {
            throw new ArgumentException($"Invalid person IdKey: {dto.PersonIdKey}", nameof(dto));
        }

        // Verify person exists
        var personExists = await context.People
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            throw new ArgumentException($"Person not found: {dto.PersonIdKey}", nameof(dto));
        }

        // Check if notification type is enabled for this person
        var isEnabled = await IsNotificationTypeEnabledAsync(
            dto.PersonIdKey,
            dto.NotificationType,
            cancellationToken);

        if (!isEnabled)
        {
            logger.LogInformation(
                "Skipping notification creation for person {PersonIdKey} - type {Type} is disabled",
                dto.PersonIdKey, dto.NotificationType);

            // Return a placeholder DTO to indicate the notification was not created
            // The caller should check if they need to handle this case
            throw new InvalidOperationException(
                $"Notification type {dto.NotificationType} is disabled for person {dto.PersonIdKey}");
        }

        var notification = new Notification
        {
            PersonId = personId,
            NotificationType = dto.NotificationType,
            Title = dto.Title,
            Message = dto.Message,
            ActionUrl = dto.ActionUrl,
            MetadataJson = dto.MetadataJson,
            IsRead = false,
            CreatedDateTime = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created notification {NotificationId} for person {PersonId} (type: {Type})",
            notification.Id, personId, dto.NotificationType);

        return MapToDto(notification);
    }

    public async Task<bool> MarkAsReadAsync(
        string idKey,
        string personIdKey,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            logger.LogWarning("Invalid notification IdKey: {IdKey}", idKey);
            return false;
        }

        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid person IdKey: {PersonIdKey}", personIdKey);
            return false;
        }

        // Ownership validation: notification must belong to the specified person
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.PersonId == personId, cancellationToken);

        if (notification == null)
        {
            logger.LogWarning(
                "Notification not found or access denied: IdKey={IdKey}, PersonIdKey={PersonIdKey}",
                idKey, personIdKey);
            return false;
        }

        if (notification.IsRead)
        {
            // Already read, no need to update
            return true;
        }

        notification.IsRead = true;
        notification.ReadDateTime = DateTime.UtcNow;
        notification.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked notification {IdKey} as read for person {PersonIdKey}",
            idKey, personIdKey);

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(
        string personIdKey,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid person IdKey: {IdKey}", personIdKey);
            return 0;
        }

        var now = DateTime.UtcNow;
        var unreadNotifications = await context.Notifications
            .Where(n => n.PersonId == personId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadNotifications.Count == 0)
        {
            return 0;
        }

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadDateTime = now;
            notification.ModifiedDateTime = now;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked {Count} notifications as read for person {PersonIdKey}",
            unreadNotifications.Count, personIdKey);

        return unreadNotifications.Count;
    }

    public async Task<bool> DeleteAsync(
        string idKey,
        string personIdKey,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            logger.LogWarning("Invalid notification IdKey: {IdKey}", idKey);
            return false;
        }

        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid person IdKey: {PersonIdKey}", personIdKey);
            return false;
        }

        // Ownership validation: notification must belong to the specified person
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.PersonId == personId, cancellationToken);

        if (notification == null)
        {
            logger.LogWarning(
                "Notification not found or access denied: IdKey={IdKey}, PersonIdKey={PersonIdKey}",
                idKey, personIdKey);
            return false;
        }

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted notification {IdKey} for person {PersonIdKey}",
            idKey, personIdKey);

        return true;
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> GetPreferencesAsync(
        string personIdKey,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid person IdKey: {IdKey}", personIdKey);
            return Array.Empty<NotificationPreferenceDto>();
        }

        // Verify person exists
        var personExists = await context.People
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            logger.LogWarning("Person not found: {PersonIdKey}", personIdKey);
            return Array.Empty<NotificationPreferenceDto>();
        }

        // Load existing preferences
        var existingPreferences = await context.NotificationPreferences
            .AsNoTracking()
            .Where(np => np.PersonId == personId)
            .ToListAsync(cancellationToken);

        var result = new List<NotificationPreferenceDto>();

        // Return preferences for all notification types
        // If no preference exists, default is enabled (IsEnabled = true)
        foreach (NotificationType type in Enum.GetValues<NotificationType>())
        {
            var preference = existingPreferences.FirstOrDefault(p => p.NotificationType == type);

            if (preference != null)
            {
                result.Add(new NotificationPreferenceDto
                {
                    IdKey = preference.IdKey,
                    NotificationType = preference.NotificationType,
                    IsEnabled = preference.IsEnabled
                });
            }
            else
            {
                // Create default preference (enabled by default)
                result.Add(new NotificationPreferenceDto
                {
                    IdKey = string.Empty, // No database record exists yet
                    NotificationType = type,
                    IsEnabled = true
                });
            }
        }

        return result;
    }

    public async Task<NotificationPreferenceDto> UpdatePreferenceAsync(
        string personIdKey,
        UpdateNotificationPreferenceDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            throw new ArgumentException($"Invalid person IdKey: {personIdKey}", nameof(personIdKey));
        }

        // Verify person exists
        var personExists = await context.People
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            throw new ArgumentException($"Person not found: {personIdKey}", nameof(personIdKey));
        }

        // Check if preference already exists
        var existing = await context.NotificationPreferences
            .FirstOrDefaultAsync(
                np => np.PersonId == personId && np.NotificationType == dto.NotificationType,
                cancellationToken);

        if (existing != null)
        {
            // Update existing preference
            existing.IsEnabled = dto.IsEnabled;
            existing.ModifiedDateTime = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Updated notification preference for person {PersonIdKey}, type {Type} to {IsEnabled}",
                personIdKey, dto.NotificationType, dto.IsEnabled);

            return new NotificationPreferenceDto
            {
                IdKey = existing.IdKey,
                NotificationType = existing.NotificationType,
                IsEnabled = existing.IsEnabled
            };
        }
        else
        {
            // Create new preference
            var preference = new NotificationPreference
            {
                PersonId = personId,
                NotificationType = dto.NotificationType,
                IsEnabled = dto.IsEnabled,
                CreatedDateTime = DateTime.UtcNow
            };

            context.NotificationPreferences.Add(preference);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created notification preference for person {PersonIdKey}, type {Type} to {IsEnabled}",
                personIdKey, dto.NotificationType, dto.IsEnabled);

            return new NotificationPreferenceDto
            {
                IdKey = preference.IdKey,
                NotificationType = preference.NotificationType,
                IsEnabled = preference.IsEnabled
            };
        }
    }

    public async Task<bool> IsNotificationTypeEnabledAsync(
        string personIdKey,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid person IdKey: {IdKey}", personIdKey);
            return true; // Default to enabled for invalid IdKey
        }

        var preference = await context.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                np => np.PersonId == personId && np.NotificationType == type,
                cancellationToken);

        // If no preference exists, default to enabled
        return preference?.IsEnabled ?? true;
    }

    /// <summary>
    /// Maps a Notification entity to a NotificationDto.
    /// </summary>
    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            IdKey = notification.IdKey,
            Guid = notification.Guid,
            NotificationType = notification.NotificationType,
            Title = notification.Title,
            Message = notification.Message,
            IsRead = notification.IsRead,
            ReadDateTime = notification.ReadDateTime,
            ActionUrl = notification.ActionUrl,
            MetadataJson = notification.MetadataJson,
            CreatedDateTime = notification.CreatedDateTime
        };
    }
}
