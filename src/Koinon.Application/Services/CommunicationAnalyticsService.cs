using System.Collections.Concurrent;
using Koinon.Application.DTOs.Communication;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

public class CommunicationAnalyticsService(
    IApplicationDbContext context,
    IMemoryCache memoryCache,
    ILogger<CommunicationAnalyticsService> logger) : ICommunicationAnalyticsService
{
    private const string CacheSummaryKeyPrefix = "comm_analytics_summary_";
    private const string RateLimitKeyPrefix = "tracking_rate_limit_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);
    private const int MaxOpensPerHour = 10;
    private const int MaxClicksPerHour = 10;

    private static readonly ConcurrentDictionary<string, (int Count, DateTime ExpiresAt)> _rateLimitCounters = new();

    private bool IsRateLimited(string rateLimitKey, int maxCount)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(RateLimitWindow);

        var result = _rateLimitCounters.AddOrUpdate(
            rateLimitKey,
            // Add: first request for this key
            (1, expiresAt),
            // Update: increment if not expired, reset if expired
            (key, existing) => existing.ExpiresAt < now
                ? (1, expiresAt)  // Expired, reset to 1
                : (existing.Count + 1, existing.ExpiresAt)  // Increment
        );

        return result.Count > maxCount;
    }

    public async Task<CommunicationAnalyticsDto?> GetCommunicationAnalyticsAsync(
        string communicationIdKey,
        CancellationToken ct = default)
    {
        var communicationId = IdKeyHelper.Decode(communicationIdKey);
        if (communicationId == 0)
        {
            return null;
        }

        // Get communication info (type, sent date) separately without loading all recipients
        var communication = await context.Communications
            .Where(c => c.Id == communicationId)
            .Select(c => new
            {
                c.CommunicationType,
                c.SentDateTime
            })
            .FirstOrDefaultAsync(ct);

        if (communication == null)
        {
            return null;
        }

        // Use a single aggregate query instead of loading all recipients into memory
        var stats = await context.CommunicationRecipients
            .Where(r => r.CommunicationId == communicationId)
            .GroupBy(r => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Pending = g.Count(r => r.Status == CommunicationRecipientStatus.Pending),
                Delivered = g.Count(r =>
                    r.Status == CommunicationRecipientStatus.Delivered ||
                    r.Status == CommunicationRecipientStatus.Opened),
                Failed = g.Count(r => r.Status == CommunicationRecipientStatus.Failed),
                Opened = g.Count(r => r.OpenedDateTime != null),
                Clicked = g.Count(r => r.ClickedDateTime != null)
            })
            .FirstOrDefaultAsync(ct);

        // Handle case where there are no recipients
        if (stats == null)
        {
            return new CommunicationAnalyticsDto
            {
                IdKey = communicationIdKey,
                CommunicationType = communication.CommunicationType.ToString(),
                TotalRecipients = 0,
                Sent = 0,
                Delivered = 0,
                Failed = 0,
                Opened = 0,
                Clicked = 0,
                OpenRate = 0,
                ClickRate = 0,
                ClickThroughRate = 0,
                DeliveryRate = 0,
                StatusBreakdown = new RecipientStatusBreakdownDto
                {
                    Pending = 0,
                    Delivered = 0,
                    Failed = 0,
                    Opened = 0
                },
                SentDateTime = communication.SentDateTime
            };
        }

        var sent = stats.Total - stats.Pending;

        decimal openRate = 0;
        decimal clickRate = 0;
        decimal clickThroughRate = 0;
        decimal deliveryRate = 0;

        if (stats.Total > 0)
        {
            deliveryRate = (decimal)stats.Delivered / stats.Total * 100;
        }

        if (communication.CommunicationType == CommunicationType.Email)
        {
            if (stats.Delivered > 0)
            {
                openRate = (decimal)stats.Opened / stats.Delivered * 100;
                clickRate = (decimal)stats.Clicked / stats.Delivered * 100;
            }

            if (stats.Opened > 0)
            {
                clickThroughRate = (decimal)stats.Clicked / stats.Opened * 100;
            }
        }

        return new CommunicationAnalyticsDto
        {
            IdKey = communicationIdKey,
            CommunicationType = communication.CommunicationType.ToString(),
            TotalRecipients = stats.Total,
            Sent = sent,
            Delivered = stats.Delivered,
            Failed = stats.Failed,
            Opened = stats.Opened,
            Clicked = stats.Clicked,
            OpenRate = Math.Round(openRate, 2),
            ClickRate = Math.Round(clickRate, 2),
            ClickThroughRate = Math.Round(clickThroughRate, 2),
            DeliveryRate = Math.Round(deliveryRate, 2),
            StatusBreakdown = new RecipientStatusBreakdownDto
            {
                Pending = stats.Pending,
                Delivered = stats.Delivered,
                Failed = stats.Failed,
                Opened = stats.Opened
            },
            SentDateTime = communication.SentDateTime
        };
    }

    public async Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        string? type = null,
        CancellationToken ct = default)
    {
        // Use ISO 8601 format (O) for cache keys to ensure timezone consistency
        var cacheKey = $"{CacheSummaryKeyPrefix}{startDate:O}_{endDate:O}_{type ?? "all"}";

        if (memoryCache.TryGetValue<AnalyticsSummaryDto>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var query = context.Communications
            .Where(c => c.CreatedDateTime >= startDate && c.CreatedDateTime <= endDate);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<CommunicationType>(type, true, out var commType))
        {
            query = query.Where(c => c.CommunicationType == commType);
        }

        var communications = await query.Include(c => c.Recipients).ToListAsync(ct);

        var totalCommunications = communications.Count;
        var totalRecipients = communications.Sum(c => c.RecipientCount);
        var totalDelivered = communications.Sum(c => c.DeliveredCount);
        var totalFailed = communications.Sum(c => c.FailedCount);
        var totalOpened = communications.Sum(c => c.OpenedCount);
        var totalClicked = communications.Sum(c => c.ClickedCount);

        decimal deliveryRate = totalRecipients > 0 ? (decimal)totalDelivered / totalRecipients * 100 : 0;
        decimal openRate = totalDelivered > 0 ? (decimal)totalOpened / totalDelivered * 100 : 0;
        decimal clickRate = totalDelivered > 0 ? (decimal)totalClicked / totalDelivered * 100 : 0;

        var emailComms = communications.Where(c => c.CommunicationType == CommunicationType.Email).ToList();
        var smsComms = communications.Where(c => c.CommunicationType == CommunicationType.Sms).ToList();

        var summary = new AnalyticsSummaryDto
        {
            TotalCommunications = totalCommunications,
            TotalRecipients = totalRecipients,
            TotalDelivered = totalDelivered,
            TotalFailed = totalFailed,
            TotalOpened = totalOpened,
            TotalClicked = totalClicked,
            DeliveryRate = Math.Round(deliveryRate, 2),
            OpenRate = Math.Round(openRate, 2),
            ClickRate = Math.Round(clickRate, 2),
            ByType = new ByTypeBreakdownDto
            {
                Email = new TypeStatsDto
                {
                    Count = emailComms.Count,
                    Recipients = emailComms.Sum(c => c.RecipientCount),
                    Delivered = emailComms.Sum(c => c.DeliveredCount),
                    Opened = emailComms.Sum(c => c.OpenedCount),
                    Clicked = emailComms.Sum(c => c.ClickedCount)
                },
                Sms = new TypeStatsDto
                {
                    Count = smsComms.Count,
                    Recipients = smsComms.Sum(c => c.RecipientCount),
                    Delivered = smsComms.Sum(c => c.DeliveredCount),
                    Opened = 0,
                    Clicked = 0
                }
            },
            StartDate = startDate,
            EndDate = endDate
        };

        memoryCache.Set(cacheKey, summary, CacheDuration);
        return summary;
    }

    public async Task RecordOpenAsync(string recipientIdKey, CancellationToken ct = default)
    {
        var recipientId = IdKeyHelper.Decode(recipientIdKey);
        if (recipientId == 0)
        {
            return;
        }

        // Check rate limit with thread-safe atomic operation
        var rateLimitKey = $"{RateLimitKeyPrefix}open_{recipientId}";
        if (IsRateLimited(rateLimitKey, MaxOpensPerHour))
        {
            logger.LogWarning("Rate limit exceeded for recipient open tracking: {RecipientId}", recipientIdKey);
            return; // Silently ignore to not break email tracking pixel
        }

        // Use atomic SQL increment to prevent race conditions
        // This updates the recipient's open count and sets opened_date_time/status on first open
        var now = DateTime.UtcNow;
        var rowsAffected = await context.Database.ExecuteSqlRawAsync(
            @"UPDATE communication_recipient
              SET open_count = open_count + 1,
                  opened_date_time = COALESCE(opened_date_time, {0}),
                  status = CASE
                      WHEN opened_date_time IS NULL THEN {1}
                      ELSE status
                  END,
                  modified_date_time = {0}
              WHERE id = {2}",
            now,
            (int)CommunicationRecipientStatus.Opened,
            recipientId);

        if (rowsAffected == 0)
        {
            logger.LogWarning("Failed to record open for recipient {RecipientId}", recipientId);
            return;
        }

        // Increment communication opened count if this was the first open
        // We need to check if the recipient's opened_date_time was just set
        var wasFirstOpen = await context.CommunicationRecipients
            .Where(r => r.Id == recipientId && r.OpenCount == 1)
            .Select(r => r.CommunicationId)
            .FirstOrDefaultAsync(ct);

        if (wasFirstOpen != 0)
        {
            await context.Database.ExecuteSqlRawAsync(
                @"UPDATE communication
                  SET opened_count = opened_count + 1,
                      modified_date_time = {0}
                  WHERE id = {1}",
                now,
                wasFirstOpen);
        }
    }

    public async Task RecordClickAsync(string recipientIdKey, CancellationToken ct = default)
    {
        var recipientId = IdKeyHelper.Decode(recipientIdKey);
        if (recipientId == 0)
        {
            return;
        }

        // Check rate limit with thread-safe atomic operation
        var rateLimitKey = $"{RateLimitKeyPrefix}click_{recipientId}";
        if (IsRateLimited(rateLimitKey, MaxClicksPerHour))
        {
            logger.LogWarning("Rate limit exceeded for recipient click tracking: {RecipientId}", recipientIdKey);
            return; // Silently ignore
        }

        // Use atomic SQL increment to prevent race conditions
        // This updates the recipient's click count and sets clicked_date_time on first click
        var now = DateTime.UtcNow;
        var rowsAffected = await context.Database.ExecuteSqlRawAsync(
            @"UPDATE communication_recipient
              SET click_count = click_count + 1,
                  clicked_date_time = COALESCE(clicked_date_time, {0}),
                  modified_date_time = {0}
              WHERE id = {1}",
            now,
            recipientId);

        if (rowsAffected == 0)
        {
            logger.LogWarning("Failed to record click for recipient {RecipientId}", recipientId);
            return;
        }

        // Increment communication clicked count if this was the first click
        // We need to check if the recipient's clicked_date_time was just set
        var wasFirstClick = await context.CommunicationRecipients
            .Where(r => r.Id == recipientId && r.ClickCount == 1)
            .Select(r => r.CommunicationId)
            .FirstOrDefaultAsync(ct);

        if (wasFirstClick != 0)
        {
            await context.Database.ExecuteSqlRawAsync(
                @"UPDATE communication
                  SET clicked_count = clicked_count + 1,
                      modified_date_time = {0}
                  WHERE id = {1}",
                now,
                wasFirstClick);
        }
    }
}
