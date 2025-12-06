using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Common;

/// <summary>
/// Batch-loads all check-in related data to eliminate N+1 query patterns.
/// All methods return pre-loaded dictionaries for O(1) lookups.
///
/// DESIGN RULES:
/// 1. Each method represents a complete data-loading operation
/// 2. No nested calls that make additional database queries
/// 3. Return dictionaries for O(1) lookups (never enumerate query results)
/// 4. Use Include/ThenInclude strategically to batch load relationships
/// 5. SelectMany + GroupBy on collections that have been pre-filtered
///
/// PERFORMANCE GOALS:
/// - LoadPersonsWithAliasesAsync: 1 query for N people, <50ms for 1000 people
/// - LoadFamilyDataAsync: 3 queries for N families, <100ms for 100 families
/// - LoadRecentAttendancesAsync: 2 queries, <50ms for 1000 people
///
/// ANTI-PATTERN (DON'T DO THIS IN SERVICES):
///   foreach (var personId in personIds) {
///       var person = await context.People.FindAsync(personId);  // N queries!
///       var aliases = await context.PersonAliases               // N queries!
///           .Where(pa => pa.PersonId == personId)
///           .ToListAsync();
///   }
///
/// CORRECT PATTERN (USE THIS):
///   var data = await dataLoader.LoadPersonsWithAliasesAsync(personIds, ct);
///   foreach (var personId in personIds) {
///       var (person, aliases) = data[personId];  // O(1) lookup, zero queries
///   }
///
/// See: ARCHITECTURAL_REVIEW_PHASE2.2.md#systematic-n-1-elimination
/// </summary>
public class CheckinDataLoader(IApplicationDbContext context, ILogger<CheckinDataLoader> logger)
{
    /// <summary>
    /// Loads persons and their primary aliases in ONE query.
    /// Returns dictionary for O(1) lookup by person ID.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: Query people, then for each person query their alias = N+1 queries
    /// - New way: Single GroupJoin query, all data loaded
    ///
    /// QUERY PLAN:
    /// 1. SELECT p.*, pa.* FROM people p
    ///    LEFT JOIN person_alias pa ON p.id = pa.person_id
    ///    WHERE p.id IN (person_ids)
    ///    AND pa.alias_person_id IS NULL
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - people.id (primary key, already exists)
    /// - person_alias.person_id
    /// - person_alias.alias_person_id
    ///
    /// USAGE EXAMPLE:
    ///   var data = await dataLoader.LoadPersonsWithAliasesAsync(
    ///       new[] { 123, 456, 789 },
    ///       cancellationToken);
    ///
    ///   // Now lookup is O(1):
    ///   if (data.TryGetValue(personId, out var personWithAlias)) {
    ///       var person = personWithAlias.Person;
    ///       var primaryAlias = personWithAlias.PrimaryAlias;
    ///   }
    /// </summary>
    public async Task<Dictionary<int, PersonWithAliasDto>> LoadPersonsWithAliasesAsync(
        IEnumerable<int> personIds,
        CancellationToken ct = default)
    {
        var ids = personIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // SINGLE optimized query
        var result = await context.People
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .GroupJoin(
                context.PersonAliases
                    .AsNoTracking()
                    .Where(pa => pa.AliasPersonId == null), // Only primary aliases
                p => p.Id,
                pa => pa.PersonId,
                (p, aliases) => new
                {
                    Person = p,
                    PrimaryAlias = aliases.FirstOrDefault()
                })
            .ToDictionaryAsync(x => x.Person.Id, x => new PersonWithAliasDto(
                x.Person,
                x.PrimaryAlias
            ), ct);

        // Log any missing data (data quality issue, not performance)
        var missing = ids.Where(id => !result.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            logger.LogWarning(
                "PersonAlias not found for {Count} people: {PersonIds}",
                missing.Count, string.Join(", ", missing.Take(5)));
        }

        return result;
    }

    /// <summary>
    /// Loads all recent attendances for given people in ONE query.
    /// Returns dictionary of personId -> list of attendances.
    ///
    /// QUERY PLAN:
    /// 1. SELECT pa.id, pa.person_id FROM person_alias pa
    ///    WHERE pa.person_id IN (person_ids)
    /// 2. SELECT a.* FROM attendance a
    ///    WHERE a.person_alias_id IN (alias_ids)
    ///    AND a.start_datetime >= fromDate
    ///    ORDER BY a.start_datetime DESC
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - person_alias.person_id
    /// - attendance.person_alias_id
    /// - attendance.start_datetime (for range query)
    ///
    /// USAGE EXAMPLE:
    ///   var recentAttendances = await dataLoader.LoadRecentAttendancesAsync(
    ///       personIds,
    ///       DateTime.UtcNow.AddDays(-30),
    ///       cancellationToken);
    ///
    ///   if (recentAttendances.TryGetValue(personId, out var attendances)) {
    ///       var isFirstTimer = attendances.Count == 0;
    ///       var lastVisit = attendances.FirstOrDefault()?.StartDateTime;
    ///   }
    /// </summary>
    public async Task<Dictionary<int, List<Attendance>>> LoadRecentAttendancesAsync(
        IEnumerable<int> personIds,
        DateTime fromDate,
        CancellationToken ct = default)
    {
        var ids = personIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // QUERY 1: Get all aliases for these people
        var personAliasIds = await context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId.HasValue && ids.Contains(pa.PersonId.Value))
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        if (personAliasIds.Count == 0)
        {
            return new();
        }

        // QUERY 2: Get all attendances for those aliases
        var attendances = await context.Attendances
            .AsNoTracking()
            .Where(a => a.PersonAliasId.HasValue &&
                       personAliasIds.Contains(a.PersonAliasId.Value) &&
                       a.StartDateTime >= fromDate)
            .Include(a => a.PersonAlias)
            .OrderByDescending(a => a.StartDateTime)
            .ToListAsync(ct);

        // Group by person ID for O(1) lookup
        return attendances
            .Where(a => a.PersonAlias != null && a.PersonAlias.PersonId.HasValue)
            .GroupBy(a => a.PersonAlias!.PersonId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());
    }

    /// <summary>
    /// Loads complete family data including all members, roles, and recent check-ins.
    /// Optimized to fetch entire family trees in minimal queries.
    ///
    /// QUERY PLAN:
    /// 1. SELECT g.*, gm.*, p.*, gr.* FROM groups g
    ///    LEFT JOIN group_member gm ON g.id = gm.group_id
    ///    LEFT JOIN person p ON gm.person_id = p.id
    ///    LEFT JOIN group_role gr ON gm.group_role_id = gr.id
    ///    WHERE g.id IN (family_ids)
    ///    AND gm.group_member_status = 'Active'
    /// 2. SELECT pa.id, pa.person_id FROM person_alias pa
    ///    WHERE pa.person_id IN (extracted_person_ids)
    /// 3. SELECT DISTINCT pa.person_id FROM attendance a
    ///    JOIN person_alias pa ON a.person_alias_id = pa.id
    ///    WHERE a.start_datetime >= recentCheckInThreshold
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - group_member.group_id
    /// - group_member.person_id
    /// - group_member.group_member_status
    /// - person_alias.person_id
    /// - attendance.start_datetime
    ///
    /// USAGE EXAMPLE:
    ///   var familyData = await dataLoader.LoadFamilyDataAsync(
    ///       new[] { familyId },
    ///       DateTime.UtcNow.AddDays(-7),
    ///       cancellationToken);
    ///
    ///   var (family, aliases, recentPeople) = familyData[familyId];
    ///   foreach (var member in family.Members) {
    ///       var hasRecentCheckIn = recentPeople.Contains(member.PersonId);
    ///   }
    /// </summary>
    public async Task<Dictionary<int, FamilyDataDto>> LoadFamilyDataAsync(
        IEnumerable<int> familyIds,
        DateTime recentCheckInThreshold,
        CancellationToken ct = default)
    {
        var ids = familyIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // QUERY 1: Families with all active members and their roles
        var families = await context.Groups
            .AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .Include(g => g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active))
                .ThenInclude(m => m.Person)
            .Include(g => g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active))
                .ThenInclude(m => m.GroupRole)
            .Include(g => g.Campus)
            .ToListAsync(ct);

        // Extract all people in all families
        var personIds = families
            .SelectMany(f => f.Members)
            .Where(m => m.Person != null)
            .Select(m => m.PersonId)
            .Distinct()
            .ToList();

        if (personIds.Count == 0)
        {
            return ids.ToDictionary(id => id, _ => new FamilyDataDto(
                Family: null!,
                PersonAliases: new(),
                RecentCheckInPeople: new HashSet<int>(),
                LastCheckInByPersonId: new Dictionary<int, DateTime>()));
        }

        // QUERY 2: All person aliases (for later lookup of attendances)
        var personAliases = await context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId.HasValue && personIds.Contains(pa.PersonId.Value))
            .ToListAsync(ct);

        // QUERY 3: People with recent check-ins and last check-in dates
        var recentCheckIns = await context.Attendances
            .AsNoTracking()
            .Where(a => a.StartDateTime >= recentCheckInThreshold &&
                       a.PersonAliasId.HasValue &&
                       a.PersonAlias != null &&
                       a.PersonAlias.PersonId.HasValue)
            .Select(a => new
            {
                PersonId = a.PersonAlias!.PersonId!.Value,
                StartDateTime = a.StartDateTime
            })
            .ToListAsync(ct);

        var recentCheckInSet = new HashSet<int>(recentCheckIns.Select(a => a.PersonId).Distinct());

        // Build dictionary of last check-in dates per person
        var lastCheckInByPerson = recentCheckIns
            .GroupBy(a => a.PersonId)
            .ToDictionary(
                g => g.Key,
                g => g.Max(a => a.StartDateTime));

        // Build result dictionary (O(1) lookup)
        var result = new Dictionary<int, FamilyDataDto>();
        foreach (var family in families)
        {
            var familyPersonAliases = personAliases
                .Where(pa => family.Members.Any(m => m.PersonId == pa.PersonId))
                .ToList();

            var familyData = new FamilyDataDto(
                Family: family,
                PersonAliases: familyPersonAliases,
                RecentCheckInPeople: recentCheckInSet,
                LastCheckInByPersonId: lastCheckInByPerson);

            // CRITICAL: Log warning if family has zero accessible members (data quality issue)
            var accessibleMemberCount = family.Members
                .Count(m => m.GroupMemberStatus == GroupMemberStatus.Active && m.Person != null);

            if (accessibleMemberCount == 0)
            {
                logger.LogWarning(
                    "Family {FamilyId} ({FamilyName}) has zero accessible active members - data integrity issue",
                    family.Id, family.Name);
            }

            result[family.Id] = familyData;
        }

        return result;
    }
}

/// <summary>
/// DTO for person with their primary alias, loaded together.
/// </summary>
public record PersonWithAliasDto(Person Person, PersonAlias? PrimaryAlias);

/// <summary>
/// DTO for complete family data including people and recent check-ins.
/// </summary>
public record FamilyDataDto(
    Group Family,
    List<PersonAlias> PersonAliases,
    HashSet<int> RecentCheckInPeople,
    Dictionary<int, DateTime> LastCheckInByPersonId);
