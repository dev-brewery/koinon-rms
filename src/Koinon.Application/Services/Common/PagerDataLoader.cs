using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Common;

/// <summary>
/// Batch-loads all pager-related data to eliminate N+1 query patterns.
/// All methods return pre-loaded dictionaries or collections for O(1) lookups.
///
/// DESIGN RULES:
/// 1. Each method represents a complete data-loading operation
/// 2. No nested calls that make additional database queries
/// 3. Return dictionaries for O(1) lookups (never enumerate query results)
/// 4. Use Include/ThenInclude strategically to batch load relationships
/// 5. AsNoTracking() for all read queries
///
/// PERFORMANCE GOALS:
/// - LoadPagerAssignmentsWithDetailsAsync: 1 query for all pagers with full graph, <100ms for 100 pagers
/// - LoadParentPhoneNumbersAsync: 2 queries for N people, <50ms for 100 people
///
/// ANTI-PATTERN (DON'T DO THIS IN SERVICES):
///   foreach (var pager in pagers) {
///       var parentPhone = await GetParentPhoneNumberAsync(pager.Attendance.PersonAlias.PersonId);  // N queries!
///   }
///
/// CORRECT PATTERN (USE THIS):
///   var phoneNumbers = await dataLoader.LoadParentPhoneNumbersAsync(personIds, ct);
///   foreach (var personId in personIds) {
///       var parentPhone = phoneNumbers[personId];  // O(1) lookup, zero queries
///   }
/// </summary>
public class PagerDataLoader(IApplicationDbContext context, ILogger<PagerDataLoader> logger)
{
    // Phone formatting constants
    private const int UsPhoneLength = 10;
    private const int UsPhoneLengthWithCountryCode = 11;
    private const string UsCountryCode = "+1";

    // Family role names that indicate adult guardians who can be paged
    private static readonly string[] AdultRoleNames = ["Adult", "Parent", "Guardian"];

    /// <summary>
    /// Loads all pager assignments for a date with complete relationship graph.
    /// Returns list with ALL related data pre-loaded (Attendance, PersonAlias, Person, Occurrence, Group, Location, Messages).
    ///
    /// WHY THIS MATTERS:
    /// - Old way: Query pagers, then for each pager query attendance, person, location, messages = N+1 queries
    /// - New way: Single Include/ThenInclude query, all data loaded
    ///
    /// QUERY PLAN:
    /// 1. SELECT pa.*, a.*, pal.*, p.*, o.*, g.*, l.*, pm.*
    ///    FROM pager_assignments pa
    ///    LEFT JOIN attendance a ON pa.attendance_id = a.id
    ///    LEFT JOIN person_alias pal ON a.person_alias_id = pal.id
    ///    LEFT JOIN people p ON pal.person_id = p.id
    ///    LEFT JOIN occurrence o ON a.occurrence_id = o.id
    ///    LEFT JOIN groups g ON o.group_id = g.id
    ///    LEFT JOIN location l ON o.location_id = l.id
    ///    LEFT JOIN pager_messages pm ON pa.id = pm.pager_assignment_id
    ///    WHERE pa.created_date_time >= date AND pa.created_date_time < date + 1 day
    ///    AND (campusId IS NULL OR pa.campus_id = campusId)
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - pager_assignments.created_date_time
    /// - pager_assignments.campus_id
    /// - attendance.id (primary key)
    /// - person_alias.id (primary key)
    /// - occurrence.id (primary key)
    /// - pager_messages.pager_assignment_id
    ///
    /// USAGE EXAMPLE:
    ///   var pagers = await dataLoader.LoadPagerAssignmentsWithDetailsAsync(
    ///       DateTime.UtcNow.Date,
    ///       campusId: 1,
    ///       cancellationToken);
    ///
    ///   // All data is pre-loaded, zero additional queries:
    ///   foreach (var pager in pagers) {
    ///       var childName = pager.Attendance?.PersonAlias?.Person?.FullName;
    ///       var locationName = pager.Attendance?.Occurrence?.Location?.Name;
    ///       var messageCount = pager.Messages.Count;
    ///   }
    /// </summary>
    public async Task<List<PagerAssignment>> LoadPagerAssignmentsWithDetailsAsync(
        DateTime date,
        int? campusId,
        CancellationToken ct = default)
    {
        var dateOnly = date.Date;
        var nextDay = dateOnly.AddDays(1);

        var query = context.PagerAssignments
            .AsNoTracking()
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.PersonAlias)
                    .ThenInclude(pal => pal!.Person)
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.Occurrence)
                    .ThenInclude(o => o!.Group)
            .Include(pa => pa.Attendance)
                .ThenInclude(a => a!.Occurrence)
                    .ThenInclude(o => o!.Location)
            .Include(pa => pa.Messages)
            .Where(pa => pa.CreatedDateTime >= dateOnly && pa.CreatedDateTime < nextDay);

        // Filter by campus if provided
        if (campusId.HasValue)
        {
            query = query.Where(pa => pa.CampusId == campusId.Value);
        }

        var result = await query
            .OrderByDescending(pa => pa.CreatedDateTime)
            .ToListAsync(ct);

        logger.LogDebug(
            "Loaded {Count} pager assignments for date {Date} (campus: {CampusId})",
            result.Count, dateOnly, campusId);

        return result;
    }

    /// <summary>
    /// Loads parent/guardian phone numbers for multiple people in batch.
    /// Returns dictionary of personId -> parent phone number (E.164 format).
    ///
    /// This consolidates the GetParentPhoneNumberAsync logic from ParentPagingService
    /// to avoid N+1 queries when getting phone numbers for multiple people.
    ///
    /// QUERY PLAN:
    /// 1. SELECT fm.person_id, fm.family_id, fm.is_primary
    ///    FROM family_members fm
    ///    WHERE fm.person_id IN (person_ids)
    ///    ORDER BY fm.is_primary DESC
    /// 2. SELECT fm.*, p.*, pn.*, fr.*
    ///    FROM family_members fm
    ///    JOIN person p ON fm.person_id = p.id
    ///    LEFT JOIN phone_numbers pn ON p.id = pn.person_id
    ///    LEFT JOIN family_roles fr ON fm.family_role_id = fr.id
    ///    WHERE fm.family_id IN (extracted_family_ids)
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - family_members.person_id
    /// - family_members.family_id
    /// - phone_numbers.person_id
    ///
    /// PHONE NUMBER LOGIC:
    /// - Finds adult family members (Adult, Parent, or Guardian roles)
    /// - Returns first SMS-enabled mobile number from adults
    /// - Formats to E.164 format (+1XXXXXXXXXX for US/Canada)
    ///
    /// USAGE EXAMPLE:
    ///   var personIds = pagerAssignments
    ///       .Select(pa => pa.Attendance?.PersonAlias?.PersonId)
    ///       .Where(id => id.HasValue)
    ///       .Select(id => id!.Value)
    ///       .ToList();
    ///
    ///   var phoneNumbers = await dataLoader.LoadParentPhoneNumbersAsync(personIds, ct);
    ///
    ///   foreach (var pager in pagerAssignments) {
    ///       var personId = pager.Attendance?.PersonAlias?.PersonId;
    ///       if (personId.HasValue && phoneNumbers.TryGetValue(personId.Value, out var phone)) {
    ///           // Use phone number, no additional query needed
    ///       }
    ///   }
    /// </summary>
    public async Task<Dictionary<int, string?>> LoadParentPhoneNumbersAsync(
        IEnumerable<int> personIds,
        CancellationToken ct = default)
    {
        var ids = personIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // QUERY 1: Get family memberships for all people
        var familyMemberships = await context.FamilyMembers
            .AsNoTracking()
            .Where(fm => ids.Contains(fm.PersonId))
            .Select(fm => new { fm.PersonId, fm.FamilyId, fm.IsPrimary })
            .ToListAsync(ct);

        // Extract unique family IDs
        var familyIds = familyMemberships
            .Select(fm => fm.FamilyId)
            .Distinct()
            .ToList();

        if (familyIds.Count == 0)
        {
            logger.LogWarning(
                "No family memberships found for {Count} people: {PersonIds}",
                ids.Count, string.Join(", ", ids.Take(5)));
            return ids.ToDictionary(id => id, _ => (string?)null);
        }

        // QUERY 2: Get all family members with phone numbers for those families
        var allFamilyMembers = await context.FamilyMembers
            .AsNoTracking()
            .Include(fm => fm.Person)
                .ThenInclude(p => p!.PhoneNumbers)
            .Include(fm => fm.FamilyRole)
            .Where(fm => familyIds.Contains(fm.FamilyId))
            .ToListAsync(ct);

        // Build result dictionary
        var result = new Dictionary<int, string?>();

        foreach (var personId in ids)
        {
            // Get this person's family membership (prefer primary)
            var membership = familyMemberships
                .Where(fm => fm.PersonId == personId)
                .OrderByDescending(fm => fm.IsPrimary)
                .FirstOrDefault();

            if (membership == null)
            {
                result[personId] = null;
                continue;
            }

            // Get adult family members from the same family
            // Uses exact role name matching to avoid false positives (e.g., "Unadulterated" matching "Adult")
            var familyAdults = allFamilyMembers
                .Where(fm => fm.FamilyId == membership.FamilyId &&
                            fm.Person != null &&
                            fm.FamilyRole != null &&
                            AdultRoleNames.Any(role =>
                                fm.FamilyRole.Name.Equals(role, StringComparison.OrdinalIgnoreCase)))
                .Select(fm => fm.Person!)
                .ToList();

            // Find first SMS-enabled mobile phone number from adults
            string? phoneNumber = null;
            foreach (var adult in familyAdults)
            {
                var mobilePhone = adult.PhoneNumbers
                    .Where(pn => pn.IsMessagingEnabled)
                    .OrderBy(pn => pn.Id) // Get the first one added
                    .FirstOrDefault();

                if (mobilePhone != null && !string.IsNullOrWhiteSpace(mobilePhone.Number))
                {
                    phoneNumber = FormatPhoneNumber(mobilePhone);
                    break;
                }
            }

            result[personId] = phoneNumber;
        }

        // Log any missing phone numbers (data quality issue, not performance)
        var missing = result.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
        if (missing.Count > 0)
        {
            logger.LogWarning(
                "No parent phone numbers found for {Count} people: {PersonIds}",
                missing.Count, string.Join(", ", missing.Take(5)));
        }

        return result;
    }

    /// <summary>
    /// Formats a phone number to E.164 format for SMS delivery.
    /// Assumes US/Canada (+1) if no country code present.
    /// </summary>
    private static string FormatPhoneNumber(PhoneNumber phoneNumber)
    {
        var number = phoneNumber.Number;
        if (!number.StartsWith('+'))
        {
            // Assume US/Canada if no country code
            var normalized = phoneNumber.NumberNormalized;
            if (normalized.Length == UsPhoneLength)
            {
                number = $"{UsCountryCode}{normalized}";
            }
            else if (normalized.Length == UsPhoneLengthWithCountryCode && normalized.StartsWith('1'))
            {
                number = $"+{normalized}";
            }
            else
            {
                number = $"{UsCountryCode}{normalized}"; // Best effort
            }
        }
        return number;
    }
}
