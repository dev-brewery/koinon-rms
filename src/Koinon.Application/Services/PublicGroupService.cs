using System.Linq.Expressions;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for public group search and discovery.
/// Provides read-only access to publicly visible group information.
/// </summary>
public class PublicGroupService(
    IApplicationDbContext context,
    ILogger<PublicGroupService> logger) : IPublicGroupService
{
    /// <summary>
    /// Gets the expression for projecting Group entities to PublicGroupDto.
    /// Shared between search and single-group retrieval to ensure consistency.
    /// Instance method to allow access to DbContext for member count subquery.
    /// </summary>
    private Expression<Func<Group, PublicGroupDto>> GetProjectToDto()
    {
        return g => new PublicGroupDto
        {
            IdKey = g.IdKey,
            Name = g.Name,
            PublicDescription = g.PublicDescription,
            GroupTypeName = g.GroupType != null ? g.GroupType.Name : null,
            CampusIdKey = g.Campus != null ? g.Campus.IdKey : null,
            CampusName = g.Campus != null ? g.Campus.Name : null,
            // Note: This subquery executes for each group - acceptable for public search use case
            MemberCount = context.GroupMembers.Count(m => m.GroupId == g.Id && m.GroupMemberStatus == GroupMemberStatus.Active),
            Capacity = g.GroupCapacity,
            MeetingDay = g.Schedule != null && g.Schedule.WeeklyDayOfWeek != null ? g.Schedule.WeeklyDayOfWeek.Value.ToString() : null,
            MeetingTime = g.Schedule != null && g.Schedule.WeeklyTimeOfDay != null
                ? TimeOnly.FromTimeSpan(g.Schedule.WeeklyTimeOfDay.Value)
                : null,
            MeetingScheduleSummary = g.Schedule != null && g.Schedule.WeeklyDayOfWeek != null && g.Schedule.WeeklyTimeOfDay != null
                ? FormatScheduleSummary(g.Schedule.WeeklyDayOfWeek.Value, g.Schedule.WeeklyTimeOfDay.Value)
                : null
        };
    }
    public async Task<PagedResult<PublicGroupDto>> SearchPublicGroupsAsync(
        PublicGroupSearchParameters parameters,
        CancellationToken ct = default)
    {
        // Input validation - clamp to valid ranges
        var pageNumber = Math.Max(1, parameters.PageNumber);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        logger.LogInformation(
            "Searching public groups with SearchTerm={SearchTerm}, GroupType={GroupType}, Campus={Campus}, DayOfWeek={DayOfWeek}, TimeOfDay={TimeOfDay}, HasOpenings={HasOpenings}, Page={Page}/{PageSize}",
            parameters.SearchTerm,
            parameters.GroupTypeIdKey,
            parameters.CampusIdKey,
            parameters.DayOfWeek,
            parameters.TimeOfDay,
            parameters.HasOpenings,
            pageNumber,
            pageSize);

        // Build base query without navigation properties for counting
        var baseQuery = context.Groups
            .AsNoTracking()
            .Where(g => g.IsPublic && g.IsActive && !g.IsArchived);

        // Apply text search on Name and PublicDescription
        // Note: Uses Like with ToLower() for case-insensitive search. Special chars (%, _) in search terms
        // will be treated as LIKE wildcards. PostgreSQL backslash escaping requires ESCAPE clause
        // which EF Core doesn't support, so we accept this limitation for simplicity.
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchPattern = $"%{parameters.SearchTerm.ToLower()}%";
            baseQuery = baseQuery.Where(g =>
                EF.Functions.Like(g.Name.ToLower(), searchPattern) ||
                (g.PublicDescription != null && EF.Functions.Like(g.PublicDescription.ToLower(), searchPattern))
            );
        }

        // Filter by GroupType
        if (!string.IsNullOrWhiteSpace(parameters.GroupTypeIdKey))
        {
            if (IdKeyHelper.TryDecode(parameters.GroupTypeIdKey, out int groupTypeId))
            {
                baseQuery = baseQuery.Where(g => g.GroupTypeId == groupTypeId);
            }
        }

        // Filter by Campus
        if (!string.IsNullOrWhiteSpace(parameters.CampusIdKey))
        {
            if (IdKeyHelper.TryDecode(parameters.CampusIdKey, out int campusId))
            {
                baseQuery = baseQuery.Where(g => g.CampusId == campusId);
            }
        }

        // Filter by DayOfWeek
        if (parameters.DayOfWeek.HasValue)
        {
            baseQuery = baseQuery.Where(g => g.Schedule != null && g.Schedule.WeeklyDayOfWeek == parameters.DayOfWeek.Value);
        }

        // Filter by TimeOfDay
        if (parameters.TimeOfDay.HasValue)
        {
            var (startTime, endTime) = GetTimeRange(parameters.TimeOfDay.Value);
            baseQuery = baseQuery.Where(g =>
                g.Schedule != null &&
                g.Schedule.WeeklyTimeOfDay != null &&
                g.Schedule.WeeklyTimeOfDay >= startTime &&
                g.Schedule.WeeklyTimeOfDay < endTime);
        }

        // Filter by HasOpenings - use database subquery to avoid loading members
        if (parameters.HasOpenings.HasValue && parameters.HasOpenings.Value)
        {
            baseQuery = baseQuery.Where(g =>
                g.GroupCapacity == null ||
                context.GroupMembers.Count(m => m.GroupId == g.Id && m.GroupMemberStatus == GroupMemberStatus.Active) < g.GroupCapacity);
        }

        // Get total count before pagination (without navigation properties)
        var totalCount = await baseQuery.CountAsync(ct);

        // Apply pagination and project to DTO with calculated member count
        var dtos = await baseQuery
            .OrderBy(g => g.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(GetProjectToDto())
            .ToListAsync(ct);

        return new PagedResult<PublicGroupDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<PublicGroupDto?> GetPublicGroupAsync(
        string groupIdKey,
        CancellationToken ct = default)
    {
        logger.LogDebug("Getting public group with IdKey={IdKey}", groupIdKey);

        if (!IdKeyHelper.TryDecode(groupIdKey, out int groupId))
        {
            return null;
        }

        // Project directly to DTO to avoid loading all members
        var dto = await context.Groups
            .AsNoTracking()
            .Where(g => g.Id == groupId && g.IsPublic && g.IsActive && !g.IsArchived)
            .Select(GetProjectToDto())
            .FirstOrDefaultAsync(ct);

        return dto;
    }

    /// <summary>
    /// Gets the time range for a given TimeRange enum value.
    /// </summary>
    private static (TimeSpan startTime, TimeSpan endTime) GetTimeRange(TimeRange timeRange)
    {
        return timeRange switch
        {
            TimeRange.Morning => (new TimeSpan(6, 0, 0), new TimeSpan(12, 0, 0)),
            TimeRange.Afternoon => (new TimeSpan(12, 0, 0), new TimeSpan(17, 0, 0)),
            TimeRange.Evening => (new TimeSpan(17, 0, 0), new TimeSpan(22, 0, 0)),
            _ => throw new ArgumentOutOfRangeException(nameof(timeRange), timeRange, "Invalid time range")
        };
    }

    /// <summary>
    /// Formats a schedule into a human-readable summary like "Sundays at 9:00 AM".
    /// </summary>
    private static string FormatScheduleSummary(DayOfWeek dayOfWeek, TimeSpan timeOfDay)
    {
        var timeOnly = TimeOnly.FromTimeSpan(timeOfDay);

        // Pluralize day of week (e.g., "Sunday" -> "Sundays")
        var dayName = $"{dayOfWeek}s";

        // Format time (e.g., "9:00 AM")
        var timeString = timeOnly.ToString("h:mm tt");

        return $"{dayName} at {timeString}";
    }
}
