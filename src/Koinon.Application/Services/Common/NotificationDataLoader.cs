using Koinon.Application.Interfaces;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Common;

/// <summary>
/// Batch-loads all notification related data to eliminate N+1 query patterns.
/// All methods return pre-loaded dictionaries for O(1) lookups.
///
/// DESIGN RULES:
/// 1. Each method represents a complete data-loading operation
/// 2. No nested calls that make additional database queries
/// 3. Return dictionaries for O(1) lookups (never enumerate query results)
/// 4. Use AsNoTracking() for all read-only queries
/// 5. Group results by person ID for efficient service-layer lookups
///
/// PERFORMANCE GOALS:
/// - LoadNotificationsForPersonsAsync: 1 query for batch of N people, <50ms for 1000 people
/// - LoadPreferencesForPersonsAsync: 1 query for batch of N people, <30ms for 1000 people
///
/// ANTI-PATTERN (DON'T DO THIS IN SERVICES):
///   foreach (var personId in personIds) {
///       var notifications = await context.Notifications              // N queries!
///           .Where(n => n.PersonId == personId)
///           .ToListAsync();
///       var preferences = await context.NotificationPreferences      // N queries!
///           .Where(p => p.PersonId == personId)
///           .ToListAsync();
///   }
///
/// CORRECT PATTERN (USE THIS):
///   var notifications = await dataLoader.LoadNotificationsForPersonsAsync(personIds, ct);
///   var preferences = await dataLoader.LoadPreferencesForPersonsAsync(personIds, ct);
///   foreach (var personId in personIds) {
///       var userNotifications = notifications.GetValueOrDefault(personId, new());  // O(1) lookup, zero queries
///       var userPreferences = preferences.GetValueOrDefault(personId, new());      // O(1) lookup, zero queries
///   }
///
/// See: ARCHITECTURAL_REVIEW_PHASE2.2.md#systematic-n-1-elimination
/// </summary>
public class NotificationDataLoader(IApplicationDbContext context, ILogger<NotificationDataLoader> logger)
{
    /// <summary>
    /// Loads all notifications for given persons in ONE query.
    /// Returns dictionary of personId -> list of notifications.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: Query notifications for each person = N queries
    /// - New way: Single WHERE IN query, all data loaded
    ///
    /// QUERY PLAN:
    /// 1. SELECT n.* FROM notification n
    ///    WHERE n.person_id IN (person_ids)
    ///    ORDER BY n.created_date_time DESC
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - notification.person_id (composite with created_date_time for optimal sorting)
    /// - notification.created_date_time (covered by composite above)
    ///
    /// USAGE EXAMPLE:
    ///   var notificationsByPerson = await dataLoader.LoadNotificationsForPersonsAsync(
    ///       new[] { 123, 456, 789 },
    ///       cancellationToken);
    ///
    ///   // Now lookup is O(1):
    ///   if (notificationsByPerson.TryGetValue(personId, out var notifications)) {
    ///       var unreadCount = notifications.Count(n => !n.IsRead);
    ///       var latest = notifications.FirstOrDefault();
    ///   }
    ///   else {
    ///       // Person has no notifications
    ///   }
    /// </summary>
    public async Task<Dictionary<int, List<Notification>>> LoadNotificationsForPersonsAsync(
        IEnumerable<int> personIds,
        CancellationToken ct = default)
    {
        var ids = personIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        logger.LogDebug(
            "Loading notifications for {Count} persons",
            ids.Count);

        // SINGLE optimized query
        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(n => ids.Contains(n.PersonId))
            .OrderByDescending(n => n.CreatedDateTime)
            .ToListAsync(ct);

        // Group by person ID for O(1) lookup
        var result = notifications
            .GroupBy(n => n.PersonId)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());

        logger.LogDebug(
            "Loaded {NotificationCount} notifications for {PersonCount} persons",
            notifications.Count,
            result.Count);

        // Log any persons with no notifications (info, not warning - this is normal)
        var personsWithoutNotifications = ids.Except(result.Keys).ToList();
        if (personsWithoutNotifications.Count > 0)
        {
            logger.LogDebug(
                "{Count} persons have no notifications",
                personsWithoutNotifications.Count);
        }

        return result;
    }

    /// <summary>
    /// Loads all notification preferences for given persons in ONE query.
    /// Returns dictionary of personId -> list of preferences.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: Query preferences for each person = N queries
    /// - New way: Single WHERE IN query, all data loaded
    ///
    /// NOTE: If a person has no preference for a notification type, it defaults to ENABLED.
    /// Services should handle missing preferences as "enabled by default".
    ///
    /// QUERY PLAN:
    /// 1. SELECT np.* FROM notification_preference np
    ///    WHERE np.person_id IN (person_ids)
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - notification_preference.person_id
    /// - notification_preference.notification_type (for lookup by type)
    ///
    /// USAGE EXAMPLE:
    ///   var preferencesByPerson = await dataLoader.LoadPreferencesForPersonsAsync(
    ///       new[] { 123, 456, 789 },
    ///       cancellationToken);
    ///
    ///   // Now lookup is O(1):
    ///   if (preferencesByPerson.TryGetValue(personId, out var preferences)) {
    ///       var checkinPref = preferences.FirstOrDefault(p => p.NotificationType == NotificationType.CheckinAlert);
    ///       var isEnabled = checkinPref?.IsEnabled ?? true;  // Default to enabled if not found
    ///   }
    ///   else {
    ///       // Person has no preferences, all notifications enabled by default
    ///   }
    /// </summary>
    public async Task<Dictionary<int, List<NotificationPreference>>> LoadPreferencesForPersonsAsync(
        IEnumerable<int> personIds,
        CancellationToken ct = default)
    {
        var ids = personIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        logger.LogDebug(
            "Loading notification preferences for {Count} persons",
            ids.Count);

        // SINGLE optimized query
        var preferences = await context.NotificationPreferences
            .AsNoTracking()
            .Where(p => ids.Contains(p.PersonId))
            .ToListAsync(ct);

        // Group by person ID for O(1) lookup
        var result = preferences
            .GroupBy(p => p.PersonId)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());

        logger.LogDebug(
            "Loaded {PreferenceCount} preferences for {PersonCount} persons",
            preferences.Count,
            result.Count);

        // Log any persons with no preferences (info, not warning - this is normal)
        var personsWithoutPreferences = ids.Except(result.Keys).ToList();
        if (personsWithoutPreferences.Count > 0)
        {
            logger.LogDebug(
                "{Count} persons have no notification preferences (defaults will apply)",
                personsWithoutPreferences.Count);
        }

        return result;
    }
}
