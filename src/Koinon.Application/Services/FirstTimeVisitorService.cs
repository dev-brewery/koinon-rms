using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for first-time visitor tracking and management.
/// Identifies and reports on people checking in for the first time.
/// </summary>
public class FirstTimeVisitorService(
    IApplicationDbContext context,
    ILogger<FirstTimeVisitorService> logger) : IFirstTimeVisitorService
{
    public async Task<IReadOnlyList<FirstTimeVisitorDto>> GetTodaysFirstTimersAsync(
        string? campusIdKey = null,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await GetFirstTimersByDateRangeAsync(today, today, campusIdKey, ct);
    }

    public async Task<IReadOnlyList<FirstTimeVisitorDto>> GetFirstTimersByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? campusIdKey = null,
        CancellationToken ct = default)
    {
        int? campusId = null;
        if (!string.IsNullOrWhiteSpace(campusIdKey))
        {
            if (!IdKeyHelper.TryDecode(campusIdKey, out int decodedCampusId))
            {
                logger.LogWarning("Invalid campus IdKey provided: {CampusIdKey}", campusIdKey);
                return Array.Empty<FirstTimeVisitorDto>();
            }
            campusId = decodedCampusId;
        }

        // Query attendances where IsFirstTime is true within the date range
        var query = context.Attendances
            .AsNoTracking()
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Group)
                    .ThenInclude(g => g!.GroupType)
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Group)
                    .ThenInclude(g => g!.Campus)
            .Include(a => a.PersonAlias)
                .ThenInclude(pa => pa!.Person)
                    .ThenInclude(p => p!.PhoneNumbers)
            .Where(a => a.IsFirstTime
                && a.Occurrence != null
                && a.Occurrence.OccurrenceDate >= startDate
                && a.Occurrence.OccurrenceDate <= endDate);

        // Apply campus filter if provided
        if (campusId.HasValue)
        {
            query = query.Where(a => a.Occurrence!.Group!.CampusId == campusId.Value);
        }

        var attendances = await query.ToListAsync(ct);

        // Get person IDs to check for follow-ups
        var personIds = attendances
            .Where(a => a.PersonAlias?.PersonId != null)
            .Select(a => a.PersonAlias!.PersonId!.Value)
            .Distinct()
            .ToList();

        // Check which persons have follow-ups
        var personIdsWithFollowUps = await context.FollowUps
            .AsNoTracking()
            .Where(f => personIds.Contains(f.PersonId))
            .Select(f => f.PersonId)
            .Distinct()
            .ToListAsync(ct);

        var followUpSet = new HashSet<int>(personIdsWithFollowUps);

        var results = new List<FirstTimeVisitorDto>();

        foreach (var attendance in attendances)
        {
            // Validate required navigation properties
            if (attendance.Occurrence?.Group == null ||
                attendance.PersonAlias?.Person == null)
            {
                logger.LogWarning(
                    "Attendance {AttendanceId} has missing required data - skipping",
                    attendance.Id);
                continue;
            }

            var person = attendance.PersonAlias.Person;
            var group = attendance.Occurrence.Group;
            var groupType = group.GroupType;

            // Get primary phone number if available
            var primaryPhone = person.PhoneNumbers?.FirstOrDefault();

            results.Add(new FirstTimeVisitorDto
            {
                PersonIdKey = person.IdKey,
                PersonName = person.FullName,
                Email = person.Email,
                PhoneNumber = primaryPhone?.Number,
                CheckInDateTime = attendance.StartDateTime,
                GroupName = group.Name,
                GroupTypeName = groupType?.Name ?? "Unknown",
                CampusName = group.Campus?.Name,
                HasFollowUp = followUpSet.Contains(person.Id)
            });
        }

        logger.LogInformation(
            "Retrieved {Count} first-time visitors between {StartDate} and {EndDate}",
            results.Count, startDate, endDate);

        return results.OrderBy(r => r.CheckInDateTime).ThenBy(r => r.PersonName).ToList();
    }

    public async Task<bool> IsFirstTimeForGroupTypeAsync(
        int personId,
        int groupTypeId,
        CancellationToken ct = default)
    {
        // Get person's aliases
        var personAliasIds = await context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId == personId)
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        if (personAliasIds.Count == 0)
        {
            return true; // No aliases found, consider it first time
        }

        // Check if person has any previous attendance at groups of this type
        var hasPreviousAttendance = await context.Attendances
            .AsNoTracking()
            .Where(a => a.PersonAliasId.HasValue &&
                       personAliasIds.Contains(a.PersonAliasId.Value))
            .Join(context.AttendanceOccurrences,
                a => a.OccurrenceId,
                o => o.Id,
                (a, o) => new { Attendance = a, Occurrence = o })
            .Join(context.Groups,
                ao => ao.Occurrence.GroupId,
                g => g.Id,
                (ao, g) => new { ao.Attendance, ao.Occurrence, Group = g })
            .AnyAsync(x => x.Group.GroupTypeId == groupTypeId, ct);

        return !hasPreviousAttendance;
    }
}
