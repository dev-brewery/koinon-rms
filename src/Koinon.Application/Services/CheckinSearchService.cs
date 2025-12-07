using System.Diagnostics;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Koinon.Application.Services.Common.GradeCalculationHelper;

namespace Koinon.Application.Services;

/// <summary>
/// Service for fast family search during check-in operations.
/// Performance-critical - all queries optimized for <100ms response time.
/// </summary>
public class CheckinSearchService(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger<CheckinSearchService> logger,
    CheckinDataLoader dataLoader)
    : AuthorizedCheckinService(context, userContext, logger), ICheckinSearchService
{
    private readonly CheckinDataLoader _dataLoader = dataLoader;

    private const int MaxSearchResults = 20;
    private const int RecentCheckInDays = 7;

    /// <summary>
    /// Masks phone numbers for logging to prevent PII exposure.
    /// Shows only the last 2 digits: "****94"
    /// </summary>
    private static string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
        {
            return "****";
        }

        // Show only last 2 digits: "****94"
        return $"****{phone[^2..]}";
    }

    public async Task<List<CheckinFamilySearchResultDto>> SearchByPhoneAsync(
        string phoneNumber,
        CancellationToken ct = default)
    {
        // SECURITY: Start timing BEFORE any input validation to prevent timing leaks
        var stopwatch = Stopwatch.StartNew();

        // Authorization check - must be authenticated
        AuthorizeAuthentication(nameof(SearchByPhoneAsync));

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return new List<CheckinFamilySearchResultDto>();
        }

        // Normalize phone number - remove all non-digits
        var normalizedPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (normalizedPhone.Length < 4)
        {
            Logger.LogWarning("Phone search requires at least 4 digits, got: {Length}", normalizedPhone.Length);
            return new List<CheckinFamilySearchResultDto>();
        }

        // Use constant-time search to prevent timing attacks
        // SECURITY: Uses 50k iterations (vs 100k for code search) because phone/name searches
        // are expected to be slower due to LIKE queries and larger result sets
        var results = await ConstantTimeHelper.SearchWithConstantTiming(
            searchOperation: async () =>
            {
                // Query phone numbers using normalized column - match last N digits or full number
                var matchingPhones = await Context.PhoneNumbers
                    .AsNoTracking()
                    .Where(p => p.NumberNormalized.EndsWith(normalizedPhone))
                    .Select(p => p.PersonId)
                    .Distinct()
                    .Take(MaxSearchResults * 2) // Get extra to account for filtering
                    .ToListAsync(ct);

                if (matchingPhones.Count == 0)
                {
                    Logger.LogInformation("Phone search found no matches for: {Phone}", MaskPhoneNumber(normalizedPhone));
                    return new List<CheckinFamilySearchResultDto>();
                }

                // Get families for these people
                var familyIds = await Context.GroupMembers
                    .AsNoTracking()
                    .Where(gm => matchingPhones.Contains(gm.PersonId) &&
                                gm.GroupMemberStatus == GroupMemberStatus.Active &&
                                gm.Group != null &&
                                gm.Group.GroupType != null &&
                                gm.Group.GroupType.IsFamilyGroupType)
                    .Select(gm => gm.GroupId)
                    .Distinct()
                    .Take(MaxSearchResults)
                    .ToListAsync(ct);

                return await GetFamiliesWithMembersAsync(familyIds, ct);
            },
            busyWorkOperation: ConstantTimeHelper.CreateHashingBusyWork(normalizedPhone, iterations: 50_000)
        );

        stopwatch.Stop();

        // Handle null result (shouldn't happen but satisfy null checker)
        var finalResults = results ?? new List<CheckinFamilySearchResultDto>();

        if (stopwatch.ElapsedMilliseconds > 100)
        {
            Logger.LogWarning(
                "Phone search exceeded 100ms target: {Elapsed}ms for {Phone}, {ResultCount} results",
                stopwatch.ElapsedMilliseconds, MaskPhoneNumber(normalizedPhone), finalResults.Count);
        }
        else
        {
            Logger.LogInformation(
                "Phone search completed in {Elapsed}ms for {Phone}, {ResultCount} results",
                stopwatch.ElapsedMilliseconds, MaskPhoneNumber(normalizedPhone), finalResults.Count);
        }

        return finalResults;
    }

    public async Task<List<CheckinFamilySearchResultDto>> SearchByNameAsync(
        string name,
        CancellationToken ct = default)
    {
        // SECURITY: Start timing BEFORE any input validation to prevent timing leaks
        var stopwatch = Stopwatch.StartNew();

        // Authorization check - must be authenticated
        AuthorizeAuthentication(nameof(SearchByNameAsync));

        if (string.IsNullOrWhiteSpace(name))
        {
            return new List<CheckinFamilySearchResultDto>();
        }

        // Escape LIKE wildcard characters to prevent SQL injection
        // User input like "A%" or "A_" should be treated as literal characters
        var escapedName = name
            .Replace("\\", "\\\\")  // Escape backslash first
            .Replace("%", "\\%")    // Escape percent wildcard
            .Replace("_", "\\_");   // Escape underscore wildcard

        // Use constant-time search to prevent timing attacks
        // SECURITY: Uses 50k iterations (vs 100k for code search) because phone/name searches
        // are expected to be slower due to LIKE queries and larger result sets
        var results = await ConstantTimeHelper.SearchWithConstantTiming(
            searchOperation: async () =>
            {
                // Search for people by name (case-insensitive)
                // Priority: StartsWith for better index usage, then Contains
                var searchTermStart = $"{escapedName}%";
                var searchTermContains = $"%{escapedName}%";

                var matchingPeople = await Context.People
                    .AsNoTracking()
                    .Where(p => !p.IsDeceased &&
                               (EF.Functions.Like(p.FirstName, searchTermStart) ||
                                EF.Functions.Like(p.LastName, searchTermStart) ||
                                (p.NickName != null && EF.Functions.Like(p.NickName, searchTermStart)) ||
                                EF.Functions.Like(p.FirstName, searchTermContains) ||
                                EF.Functions.Like(p.LastName, searchTermContains) ||
                                (p.NickName != null && EF.Functions.Like(p.NickName, searchTermContains))))
                    .Select(p => p.Id)
                    .Take(MaxSearchResults * 2) // Get extra to account for filtering
                    .ToListAsync(ct);

                if (matchingPeople.Count == 0)
                {
                    Logger.LogInformation("Name search found no matches for: {Name}", name);
                    return new List<CheckinFamilySearchResultDto>();
                }

                // Get families for these people
                var familyIds = await Context.GroupMembers
                    .AsNoTracking()
                    .Where(gm => matchingPeople.Contains(gm.PersonId) &&
                                gm.GroupMemberStatus == GroupMemberStatus.Active &&
                                gm.Group != null &&
                                gm.Group.GroupType != null &&
                                gm.Group.GroupType.IsFamilyGroupType)
                    .Select(gm => gm.GroupId)
                    .Distinct()
                    .Take(MaxSearchResults)
                    .ToListAsync(ct);

                return await GetFamiliesWithMembersAsync(familyIds, ct);
            },
            busyWorkOperation: ConstantTimeHelper.CreateHashingBusyWork(name, iterations: 50_000)
        );

        stopwatch.Stop();

        // Handle null result (shouldn't happen but satisfy null checker)
        var finalResults = results ?? new List<CheckinFamilySearchResultDto>();

        if (stopwatch.ElapsedMilliseconds > 100)
        {
            Logger.LogWarning(
                "Name search exceeded 100ms target: {Elapsed}ms for {Name}, {ResultCount} results",
                stopwatch.ElapsedMilliseconds, name, finalResults.Count);
        }
        else
        {
            Logger.LogInformation(
                "Name search completed in {Elapsed}ms for {Name}, {ResultCount} results",
                stopwatch.ElapsedMilliseconds, name, finalResults.Count);
        }

        return finalResults;
    }

    public async Task<CheckinFamilySearchResultDto?> SearchByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        // SECURITY: Start timing BEFORE any input validation to prevent timing leaks
        var stopwatch = Stopwatch.StartNew();

        // Authorization check - must be authenticated
        AuthorizeAuthentication(nameof(SearchByCodeAsync));

        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        // Normalize code (uppercase, trim)
        var normalizedCode = code.Trim().ToUpperInvariant();

        // Get today's date (UTC)
        var today = DateTime.UtcNow.Date;

        // Use constant-time search to prevent timing attacks
        // SECURITY: Uses 100k iterations (vs 50k for phone/name) because code searches are fast
        // (indexed lookup by code + date) and need more busy work to mask timing differences
        var result = await ConstantTimeHelper.SearchWithConstantTiming(
            searchOperation: async () =>
            {
                // Find attendance code issued today
                var attendanceCode = await Context.AttendanceCodes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ac =>
                        ac.Code == normalizedCode &&
                        ac.IssueDateTime.Date == today,
                        ct);

                if (attendanceCode == null)
                {
                    return null;
                }

                // Find most recent attendance using this code
                var attendance = await Context.Attendances
                    .AsNoTracking()
                    .Where(a => a.AttendanceCodeId == attendanceCode.Id)
                    .OrderByDescending(a => a.StartDateTime)
                    .FirstOrDefaultAsync(ct);

                if (attendance?.PersonAliasId == null)
                {
                    Logger.LogWarning("Attendance code {Code} found but no associated person", normalizedCode);
                    return null;
                }

                // Get the person who checked in
                var personAlias = await Context.PersonAliases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pa => pa.Id == attendance.PersonAliasId.Value, ct);

                if (personAlias == null)
                {
                    Logger.LogWarning("PersonAlias {AliasId} not found for attendance code {Code}",
                        attendance.PersonAliasId.Value, normalizedCode);
                    return null;
                }

                // Get the person's family
                var person = await Context.People
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == personAlias.PersonId, ct);

                if (person?.PrimaryFamilyId == null)
                {
                    Logger.LogWarning("Person {PersonId} has no primary family for code {Code}",
                        personAlias.PersonId, normalizedCode);
                    return null;
                }

                // Get family with members using batch loader
                var familyData = await _dataLoader.LoadFamilyDataAsync(
                    new[] { person.PrimaryFamilyId.Value },
                    DateTime.UtcNow.AddDays(-RecentCheckInDays),
                    ct);

                if (!familyData.TryGetValue(person.PrimaryFamilyId.Value, out var data))
                {
                    return null;
                }

                return BuildFamilySearchResult(data);
            },
            busyWorkOperation: ConstantTimeHelper.CreateHashingBusyWork(normalizedCode, iterations: 100_000)
        );

        stopwatch.Stop();

        // Always log with consistent timing to prevent timing attacks
        // (valid codes and invalid codes should take similar time)
        if (stopwatch.ElapsedMilliseconds > 100)
        {
            Logger.LogWarning(
                "Code search exceeded 100ms target: {Elapsed}ms for {Code}",
                stopwatch.ElapsedMilliseconds, normalizedCode);
        }
        else
        {
            Logger.LogInformation(
                "Code search completed in {Elapsed}ms for {Code}, found: {Found}",
                stopwatch.ElapsedMilliseconds, normalizedCode, result != null);
        }

        return result;
    }

    public async Task<List<CheckinFamilySearchResultDto>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<CheckinFamilySearchResultDto>();
        }

        // Detect query type
        var trimmedQuery = query.Trim();

        // If all digits and 4+ characters, treat as phone search
        if (trimmedQuery.All(char.IsDigit) && trimmedQuery.Length >= 4)
        {
            Logger.LogDebug("Auto-detected phone search for: {Query}", trimmedQuery);
            return await SearchByPhoneAsync(trimmedQuery, ct);
        }

        // If 3-4 alphanumeric characters, try code search first
        if (trimmedQuery.Length <= 4 && trimmedQuery.All(char.IsLetterOrDigit))
        {
            Logger.LogDebug("Auto-detected code search for: {Query}", trimmedQuery);
            var codeResult = await SearchByCodeAsync(trimmedQuery, ct);
            if (codeResult != null)
            {
                return new List<CheckinFamilySearchResultDto> { codeResult };
            }
        }

        // Default to name search
        Logger.LogDebug("Auto-detected name search for: {Query}", trimmedQuery);
        return await SearchByNameAsync(trimmedQuery, ct);
    }

    /// <summary>
    /// Retrieves full family information with all active members.
    /// Uses CheckinDataLoader to batch load all data and eliminate N+1 patterns.
    /// </summary>
    private async Task<List<CheckinFamilySearchResultDto>> GetFamiliesWithMembersAsync(
        IEnumerable<int> familyIds,
        CancellationToken ct = default)
    {
        var familyIdList = familyIds.ToList();
        if (familyIdList.Count == 0)
        {
            return new List<CheckinFamilySearchResultDto>();
        }

        // Get recent check-in date threshold
        var recentCheckInDate = DateTime.UtcNow.AddDays(-RecentCheckInDays);

        // BATCH LOAD: Use data loader to get all family data in minimal queries
        var familyDataDict = await _dataLoader.LoadFamilyDataAsync(familyIdList, recentCheckInDate, ct);

        // Build results from loaded data
        var results = new List<CheckinFamilySearchResultDto>();

        foreach (var familyId in familyIdList)
        {
            if (!familyDataDict.TryGetValue(familyId, out var familyData))
            {
                continue; // Skip families that couldn't be loaded
            }

            // SECURITY: Skip families where Family data is null (data loader returned incomplete result)
            if (familyData.Family == null)
            {
                Logger.LogWarning("Family {FamilyId} loaded with null Family object - skipping", familyId);
                continue;
            }

            results.Add(BuildFamilySearchResult(familyData));
        }

        return results;
    }

    /// <summary>
    /// Builds a CheckinFamilySearchResultDto from loaded family data.
    /// Centralizes the mapping logic used by both code search and other search methods.
    /// </summary>
    private static CheckinFamilySearchResultDto BuildFamilySearchResult(FamilyDataDto familyData)
    {
        // SECURITY: Validate loaded data before processing
        if (familyData.Family == null)
        {
            throw new InvalidOperationException("Family data is null - data loader returned incomplete result");
        }

        var family = familyData.Family;
        var recentCheckInPeople = familyData.RecentCheckInPeople;
        var lastCheckInByPerson = familyData.LastCheckInByPersonId;

        var members = new List<CheckinFamilyMemberDto>();

        foreach (var groupMember in family.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active))
        {
            if (groupMember.Person == null)
            {
                continue; // Skip if person not loaded
            }

            // Use pre-loaded check-in data (O(1) HashSet lookup)
            var hasRecentCheckIn = recentCheckInPeople.Contains(groupMember.PersonId);

            // Get last check-in date (O(1) dictionary lookup)
            lastCheckInByPerson.TryGetValue(groupMember.PersonId, out var lastCheckIn);

            // Calculate age
            int? age = null;
            if (groupMember.Person.BirthDate.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                age = today.Year - groupMember.Person.BirthDate.Value.Year;
                if (groupMember.Person.BirthDate.Value > today.AddYears(-age.Value))
                {
                    age--;
                }
            }

            // Determine if child (typically under 18 or based on role)
            var isChild = age.HasValue && age.Value < 18;

            // Calculate grade from graduation year
            var grade = CalculateGrade(groupMember.Person.GraduationYear);

            members.Add(new CheckinFamilyMemberDto
            {
                PersonIdKey = groupMember.Person.IdKey,
                FullName = groupMember.Person.FullName,
                FirstName = groupMember.Person.FirstName,
                LastName = groupMember.Person.LastName,
                NickName = groupMember.Person.NickName,
                Age = age,
                Gender = groupMember.Person.Gender.ToString(),
                PhotoUrl = null, // Photo URLs require BinaryFile entity (not yet implemented)
                RoleName = groupMember.GroupRole?.Name ?? "Member",
                IsChild = isChild,
                HasRecentCheckIn = hasRecentCheckIn,
                LastCheckIn = lastCheckIn == default ? null : lastCheckIn,
                Grade = grade
            });
        }

        // Sort members: adults first, then by age descending
        members = members
            .OrderBy(m => m.IsChild)
            .ThenByDescending(m => m.Age ?? 0)
            .ToList();

        // Count recent check-ins for this family
        var recentCheckInCount = family.Members
            .Where(m => m.GroupMemberStatus == GroupMemberStatus.Active)
            .Count(m => recentCheckInPeople.Contains(m.PersonId));

        return new CheckinFamilySearchResultDto
        {
            FamilyIdKey = family.IdKey,
            FamilyName = family.Name,
            AddressSummary = null, // Address summaries require Location entity (not yet implemented)
            CampusName = family.Campus?.Name,
            Members = members,
            RecentCheckInCount = recentCheckInCount
        };
    }
}
