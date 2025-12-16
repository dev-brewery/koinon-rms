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

        // Execute all count queries in parallel for better performance
        var totalPeopleTask = context.People
            .CountAsync(p => !p.IsDeceased, cancellationToken);

        var totalFamiliesTask = context.Families
            .CountAsync(f => f.IsActive, cancellationToken);

        var activeGroupsTask = context.Groups
            .CountAsync(g => g.IsActive && !g.IsArchived, cancellationToken);

        var activeSchedulesTask = context.Schedules
            .CountAsync(s => s.IsActive, cancellationToken);

        // Check-ins for today (attendance records where StartDateTime is today in UTC)
        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var todayEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var todayCheckInsTask = context.Attendances
            .CountAsync(a => a.StartDateTime >= todayStart && a.StartDateTime < todayEnd, cancellationToken);

        // Check-ins for same day last week
        var lastWeekStart = lastWeek.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var lastWeekEnd = lastWeek.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var lastWeekCheckInsTask = context.Attendances
            .CountAsync(a => a.StartDateTime >= lastWeekStart && a.StartDateTime < lastWeekEnd, cancellationToken);

        // Get upcoming schedules (next 5 active weekly schedules)
        var upcomingSchedulesTask = GetUpcomingSchedulesAsync(now, cancellationToken);

        // Await all tasks
        await Task.WhenAll(
            totalPeopleTask,
            totalFamiliesTask,
            activeGroupsTask,
            activeSchedulesTask,
            todayCheckInsTask,
            lastWeekCheckInsTask,
            upcomingSchedulesTask);

        var stats = new DashboardStatsDto
        {
            TotalPeople = await totalPeopleTask,
            TotalFamilies = await totalFamiliesTask,
            ActiveGroups = await activeGroupsTask,
            ActiveSchedules = await activeSchedulesTask,
            TodayCheckIns = await todayCheckInsTask,
            LastWeekCheckIns = await lastWeekCheckInsTask,
            UpcomingSchedules = await upcomingSchedulesTask
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
}
