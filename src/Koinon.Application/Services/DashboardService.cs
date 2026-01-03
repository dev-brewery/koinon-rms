using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for retrieving dashboard statistics and metrics.
/// Aggregates data from multiple entities for admin dashboard displays.
/// </summary>
public class DashboardService(
    IApplicationDbContext context,
    ILogger<DashboardService> logger) : IDashboardService
{
    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving dashboard statistics");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var lastWeek = today.AddDays(-7);

        // Execute queries sequentially - DbContext is NOT thread-safe
        var totalPeople = await context.People
            .CountAsync(p => !p.IsDeceased, cancellationToken);

        var totalFamilies = await context.Families
            .CountAsync(f => f.IsActive, cancellationToken);

        var activeGroups = await context.Groups
            .CountAsync(g => g.IsActive && !g.IsArchived, cancellationToken);

        var activeSchedules = await context.Schedules
            .CountAsync(s => s.IsActive, cancellationToken);

        // Check-ins for today (attendance records where StartDateTime is today in UTC)
        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var todayEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var todayCheckIns = await context.Attendances
            .CountAsync(a => a.StartDateTime >= todayStart && a.StartDateTime < todayEnd, cancellationToken);

        // Check-ins for same day last week
        var lastWeekStart = lastWeek.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var lastWeekEnd = lastWeek.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var lastWeekCheckIns = await context.Attendances
            .CountAsync(a => a.StartDateTime >= lastWeekStart && a.StartDateTime < lastWeekEnd, cancellationToken);

        // Get upcoming schedules (next 5 active weekly schedules)
        var upcomingSchedules = await GetUpcomingSchedulesAsync(now, cancellationToken);

        // Get giving statistics
        var givingStats = await GetGivingStatsAsync(now, cancellationToken);

        // Get communications statistics
        var communicationsStats = await GetCommunicationsStatsAsync(now, cancellationToken);

        var stats = new DashboardStatsDto
        {
            TotalPeople = totalPeople,
            TotalFamilies = totalFamilies,
            ActiveGroups = activeGroups,
            ActiveSchedules = activeSchedules,
            TodayCheckIns = todayCheckIns,
            LastWeekCheckIns = lastWeekCheckIns,
            UpcomingSchedules = upcomingSchedules,
            GivingStats = givingStats,
            CommunicationsStats = communicationsStats
        };

        logger.LogInformation(
            "Dashboard statistics retrieved: People={TotalPeople}, Families={TotalFamilies}, Groups={ActiveGroups}, " +
            "TodayCheckIns={TodayCheckIns}, LastWeekCheckIns={LastWeekCheckIns}, ActiveSchedules={ActiveSchedules}",
            stats.TotalPeople, stats.TotalFamilies, stats.ActiveGroups,
            stats.TodayCheckIns, stats.LastWeekCheckIns, stats.ActiveSchedules);

        return stats;
    }

    private async Task<List<UpcomingScheduleDto>> GetUpcomingSchedulesAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Get active weekly schedules with check-in times configured
        var activeSchedules = await context.Schedules
            .AsNoTracking()
            .Where(s => s.IsActive && s.WeeklyDayOfWeek.HasValue && s.WeeklyTimeOfDay.HasValue)
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Name)
            .Take(10) // Get top 10 to filter down to next 5 occurrences
            .Select(s => new
            {
                s.IdKey,
                s.Name,
                s.WeeklyDayOfWeek,
                s.WeeklyTimeOfDay,
                s.CheckInStartOffsetMinutes
            })
            .ToListAsync(cancellationToken);

        var upcomingSchedules = new List<UpcomingScheduleDto>();

        foreach (var schedule in activeSchedules)
        {
            if (!schedule.WeeklyDayOfWeek.HasValue || !schedule.WeeklyTimeOfDay.HasValue)
            {
                continue;
            }

            // Calculate next occurrence for this weekly schedule
            var nextOccurrence = CalculateNextOccurrence(
                now,
                schedule.WeeklyDayOfWeek.Value,
                schedule.WeeklyTimeOfDay.Value);

            // Calculate when check-in opens
            var checkInStartOffset = schedule.CheckInStartOffsetMinutes ?? 60; // Default to 60 minutes before
            var checkInOpenTime = nextOccurrence.AddMinutes(-checkInStartOffset);

            // Calculate minutes until check-in
            var minutesUntilCheckIn = (int)(checkInOpenTime - now).TotalMinutes;

            upcomingSchedules.Add(new UpcomingScheduleDto
            {
                IdKey = schedule.IdKey,
                Name = schedule.Name,
                NextOccurrence = nextOccurrence,
                MinutesUntilCheckIn = minutesUntilCheckIn
            });
        }

        // Return next 5 occurrences sorted by next occurrence time
        return upcomingSchedules
            .OrderBy(s => s.NextOccurrence)
            .Take(5)
            .ToList();
    }

    private static DateTime CalculateNextOccurrence(DateTime fromDateTime, DayOfWeek targetDayOfWeek, TimeSpan targetTime)
    {
        var currentDate = DateOnly.FromDateTime(fromDateTime);
        var currentTime = TimeOnly.FromDateTime(fromDateTime);
        var targetTimeOnly = TimeOnly.FromTimeSpan(targetTime);

        // Start with today
        var candidateDate = currentDate;
        var currentDayOfWeek = fromDateTime.DayOfWeek;

        // If today is the target day and the time hasn't passed yet, use today
        if (currentDayOfWeek == targetDayOfWeek && currentTime < targetTimeOnly)
        {
            return candidateDate.ToDateTime(targetTimeOnly, DateTimeKind.Utc);
        }

        // Otherwise, find the next occurrence of the target day
        var daysUntilTarget = ((int)targetDayOfWeek - (int)currentDayOfWeek + 7) % 7;
        if (daysUntilTarget == 0)
        {
            daysUntilTarget = 7; // Move to next week if same day but time has passed
        }

        candidateDate = candidateDate.AddDays(daysUntilTarget);
        return candidateDate.ToDateTime(targetTimeOnly, DateTimeKind.Utc);
    }

    private async Task<GivingStatsDto> GetGivingStatsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Calculate Month-to-Date total from contribution details
        var mtdTotal = await context.ContributionDetails
            .AsNoTracking()
            .Where(cd => cd.Contribution != null && cd.Contribution.TransactionDateTime >= currentMonth)
            .SumAsync(cd => cd.Amount, cancellationToken);

        // Calculate Year-to-Date total from contribution details
        var ytdTotal = await context.ContributionDetails
            .AsNoTracking()
            .Where(cd => cd.Contribution != null && cd.Contribution.TransactionDateTime >= currentYear)
            .SumAsync(cd => cd.Amount, cancellationToken);

        // Get last 5 open/closed batches
        var recentBatches = await context.ContributionBatches
            .AsNoTracking()
            .Where(cb => cb.Status == Domain.Enums.BatchStatus.Open || cb.Status == Domain.Enums.BatchStatus.Closed)
            .OrderByDescending(cb => cb.BatchDate)
            .Take(5)
            .Select(cb => new
            {
                cb.IdKey,
                cb.Name,
                cb.BatchDate,
                cb.Status,
                cb.Id
            })
            .ToListAsync(cancellationToken);

        // Calculate totals for each batch using single query with GroupBy
        var batchIds = recentBatches.Select(b => b.Id).ToList();

        var batchTotals = await context.ContributionDetails
            .AsNoTracking()
            .Where(cd => cd.Contribution != null && cd.Contribution.BatchId.HasValue && batchIds.Contains(cd.Contribution.BatchId.Value))
            .GroupBy(cd => cd.Contribution!.BatchId!.Value)
            .Select(g => new { BatchId = g.Key, Total = g.Sum(cd => cd.Amount) })
            .ToListAsync(cancellationToken);

        var batchTotalLookup = batchTotals.ToDictionary(bt => bt.BatchId, bt => bt.Total);

        var batchDtos = recentBatches.Select(batch => new DashboardBatchDto
        {
            IdKey = batch.IdKey,
            Name = batch.Name,
            BatchDate = batch.BatchDate,
            Status = batch.Status.ToString(),
            Total = batchTotalLookup.TryGetValue(batch.Id, out var total) ? total : 0m
        }).ToList();

        return new GivingStatsDto
        {
            MonthToDateTotal = mtdTotal,
            YearToDateTotal = ytdTotal,
            RecentBatches = batchDtos
        };
    }

    private async Task<CommunicationsStatsDto> GetCommunicationsStatsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var sevenDaysAgo = now.AddDays(-7);

        // Count pending communications
        var pendingCount = await context.Communications
            .AsNoTracking()
            .CountAsync(c => c.Status == Domain.Enums.CommunicationStatus.Pending, cancellationToken);

        // Count sent communications in last 7 days
        var sentThisWeekCount = await context.Communications
            .AsNoTracking()
            .CountAsync(c => c.Status == Domain.Enums.CommunicationStatus.Sent
                && c.CreatedDateTime >= sevenDaysAgo, cancellationToken);

        // Get last 5 communications
        var recentCommunications = await context.Communications
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedDateTime)
            .Take(5)
            .Select(c => new CommunicationSummaryDto
            {
                IdKey = c.IdKey,
                CommunicationType = c.CommunicationType.ToString(),
                Status = c.Status.ToString(),
                Subject = c.Subject,
                RecipientCount = c.RecipientCount,
                DeliveredCount = c.DeliveredCount,
                FailedCount = c.FailedCount,
                ScheduledDateTime = c.ScheduledDateTime,
                CreatedDateTime = c.CreatedDateTime,
                SentDateTime = c.SentDateTime
            })
            .ToListAsync(cancellationToken);

        return new CommunicationsStatsDto
        {
            PendingCount = pendingCount,
            SentThisWeekCount = sentThisWeekCount,
            RecentCommunications = recentCommunications
        };
    }
}
