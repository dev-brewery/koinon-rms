using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for generating attendance analytics and reports.
/// Provides summary statistics, trends, and group-based breakdowns with efficient database queries.
/// </summary>
public class AttendanceAnalyticsService(
    IApplicationDbContext context,
    ILogger<AttendanceAnalyticsService> logger) : IAttendanceAnalyticsService
{
    public async Task<AttendanceAnalyticsDto> GetSummaryAsync(
        AttendanceQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving attendance summary analytics");

        var (startDate, endDate) = GetDateRange(options);
        var query = BuildBaseQuery(options, startDate, endDate);

        // Execute single aggregated query to avoid N+1 pattern
        var stats = await query
            .GroupBy(a => 1)
            .Select(g => new
            {
                TotalAttendance = g.Count(),
                UniqueAttendees = g.Where(a => a.PersonAliasId.HasValue)
                    .Select(a => a.PersonAliasId)
                    .Distinct()
                    .Count(),
                FirstTimeVisitors = g.Count(a => a.IsFirstTime),
                TotalOccurrences = g.Select(a => a.OccurrenceId).Distinct().Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Handle case where no data exists
        if (stats == null)
        {
            return new AttendanceAnalyticsDto(0, 0, 0, 0, 0m, startDate, endDate);
        }

        var totalAttendance = stats.TotalAttendance;
        var uniqueAttendees = stats.UniqueAttendees;
        var firstTimeVisitors = stats.FirstTimeVisitors;
        var returningVisitors = totalAttendance - firstTimeVisitors;
        var totalOccurrences = stats.TotalOccurrences;

        var averageAttendance = totalOccurrences > 0
            ? (decimal)totalAttendance / totalOccurrences
            : 0m;

        logger.LogInformation(
            "Attendance summary: Total={TotalAttendance}, Unique={UniqueAttendees}, " +
            "FirstTime={FirstTimeVisitors}, Returning={ReturningVisitors}, Average={AverageAttendance:F2}",
            totalAttendance, uniqueAttendees, firstTimeVisitors, returningVisitors, averageAttendance);

        return new AttendanceAnalyticsDto(
            totalAttendance,
            uniqueAttendees,
            firstTimeVisitors,
            returningVisitors,
            averageAttendance,
            startDate,
            endDate);
    }

    public async Task<IReadOnlyList<AttendanceTrendDto>> GetTrendsAsync(
        AttendanceQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving attendance trends with GroupBy={GroupBy}", options.GroupBy);

        var (startDate, endDate) = GetDateRange(options);
        var query = BuildBaseQuery(options, startDate, endDate);

        // Group by the specified time period in the database
        // Use anonymous type for EF Core translation, then convert to DTO
        var groupedData = options.GroupBy switch
        {
            GroupBy.Day => await query
                .GroupBy(a => a.Occurrence!.OccurrenceDate)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalAttendance = g.Count(),
                    FirstTimeCount = g.Count(a => a.IsFirstTime),
                    ReturningCount = g.Count(a => !a.IsFirstTime)
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken),

            GroupBy.Week => await query
                .GroupBy(a => a.Occurrence!.OccurrenceDate.AddDays(-(int)a.Occurrence.OccurrenceDate.DayOfWeek))
                .Select(g => new
                {
                    Date = g.Key,
                    TotalAttendance = g.Count(),
                    FirstTimeCount = g.Count(a => a.IsFirstTime),
                    ReturningCount = g.Count(a => !a.IsFirstTime)
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken),

            GroupBy.Month => await query
                .GroupBy(a => new DateOnly(a.Occurrence!.OccurrenceDate.Year, a.Occurrence.OccurrenceDate.Month, 1))
                .Select(g => new
                {
                    Date = g.Key,
                    TotalAttendance = g.Count(),
                    FirstTimeCount = g.Count(a => a.IsFirstTime),
                    ReturningCount = g.Count(a => !a.IsFirstTime)
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken),

            GroupBy.Year => await query
                .GroupBy(a => new DateOnly(a.Occurrence!.OccurrenceDate.Year, 1, 1))
                .Select(g => new
                {
                    Date = g.Key,
                    TotalAttendance = g.Count(),
                    FirstTimeCount = g.Count(a => a.IsFirstTime),
                    ReturningCount = g.Count(a => !a.IsFirstTime)
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken),

            _ => await query
                .GroupBy(a => a.Occurrence!.OccurrenceDate)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalAttendance = g.Count(),
                    FirstTimeCount = g.Count(a => a.IsFirstTime),
                    ReturningCount = g.Count(a => !a.IsFirstTime)
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken)
        };

        // Convert to DTOs in memory
        var trends = groupedData
            .Select(g => new AttendanceTrendDto(
                g.Date,
                g.TotalAttendance,
                g.FirstTimeCount,
                g.ReturningCount))
            .ToList();

        logger.LogInformation("Retrieved {TrendCount} trend data points", trends.Count);

        return trends;
    }

    public async Task<IReadOnlyList<AttendanceByGroupDto>> GetByGroupAsync(
        AttendanceQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving attendance by group");

        var (startDate, endDate) = GetDateRange(options);
        var query = BuildBaseQuery(options, startDate, endDate);

        // Group by occurrence group and calculate statistics
        var groupedData = await query
            .Where(a => a.Occurrence!.GroupId.HasValue)
            .GroupBy(a => new
            {
                GroupId = a.Occurrence!.GroupId!.Value,
                GroupName = a.Occurrence!.Group!.Name,
                GroupTypeName = a.Occurrence!.Group!.GroupType!.Name
            })
            .Select(g => new
            {
                g.Key.GroupId,
                g.Key.GroupName,
                g.Key.GroupTypeName,
                TotalAttendance = g.Count(),
                // Count distinct PersonAliasId values efficiently
                UniqueAttendees = g.Select(a => a.PersonAliasId).Where(id => id != null).Distinct().Count()
            })
            .OrderByDescending(g => g.TotalAttendance)
            .ToListAsync(cancellationToken);

        // Convert to DTOs with IdKey encoding
        var results = groupedData
            .Select(g => new AttendanceByGroupDto(
                IdKeyHelper.Encode(g.GroupId),
                g.GroupName,
                g.GroupTypeName,
                g.TotalAttendance,
                g.UniqueAttendees))
            .ToList();

        logger.LogInformation("Retrieved attendance for {GroupCount} groups", results.Count);

        return results;
    }

    /// <summary>
    /// Builds the base query with common filters applied.
    /// </summary>
    private IQueryable<Domain.Entities.Attendance> BuildBaseQuery(
        AttendanceQueryOptions options,
        DateOnly startDate,
        DateOnly endDate)
    {
        var query = context.Attendances.AsNoTracking();

        // Filter by date range using Occurrence.OccurrenceDate
        query = query
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Group)
                    .ThenInclude(g => g!.GroupType)
            .Where(a => a.Occurrence != null && a.Occurrence.OccurrenceDate >= startDate && a.Occurrence.OccurrenceDate <= endDate);

        // Filter by campus if specified
        if (!string.IsNullOrWhiteSpace(options.CampusIdKey))
        {
            var campusId = IdKeyHelper.Decode(options.CampusIdKey);
            query = query.Where(a => a.CampusId == campusId);
        }

        // Filter by group type if specified
        if (!string.IsNullOrWhiteSpace(options.GroupTypeIdKey))
        {
            var groupTypeId = IdKeyHelper.Decode(options.GroupTypeIdKey);
            query = query.Where(a => a.Occurrence != null && a.Occurrence.Group != null && a.Occurrence.Group.GroupTypeId == groupTypeId);
        }

        // Filter by specific group if specified
        if (!string.IsNullOrWhiteSpace(options.GroupIdKey))
        {
            var groupId = IdKeyHelper.Decode(options.GroupIdKey);
            query = query.Where(a => a.Occurrence!.GroupId == groupId);
        }

        return query;
    }

    /// <summary>
    /// Gets the date range for the query, using defaults if not specified.
    /// </summary>
    private static (DateOnly StartDate, DateOnly EndDate) GetDateRange(AttendanceQueryOptions options)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = options.StartDate ?? today.AddDays(-30);
        var endDate = options.EndDate ?? today;

        return (startDate, endDate);
    }
}
