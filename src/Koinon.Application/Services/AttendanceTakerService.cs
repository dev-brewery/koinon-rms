using System.Diagnostics;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for manual attendance taking operations.
/// Handles staff marking attendance during services, bulk entry, and historical recording.
/// Performance target: <100ms for single mark, <500ms for family mark.
/// </summary>
public class AttendanceTakerService(
    IApplicationDbContext context,
    ILogger<AttendanceTakerService> logger)
    : IAttendanceTakerService
{
    public async Task<MarkAttendanceResultDto> MarkAttendedAsync(
        string occurrenceIdKey,
        string personIdKey,
        string? note = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Decode IDs
            if (!IdKeyHelper.TryDecode(occurrenceIdKey, out int occurrenceId))
            {
                return new MarkAttendanceResultDto(
                    Success: false,
                    ErrorMessage: "Invalid occurrence ID");
            }

            if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
            {
                return new MarkAttendanceResultDto(
                    Success: false,
                    ErrorMessage: "Invalid person ID");
            }

            // Verify occurrence exists
            var occurrence = await context.AttendanceOccurrences
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct);

            if (occurrence == null)
            {
                return new MarkAttendanceResultDto(
                    Success: false,
                    ErrorMessage: "Occurrence not found");
            }

            // Get person's primary alias
            var personAlias = await context.PersonAliases
                .AsNoTracking()
                .FirstOrDefaultAsync(pa => pa.PersonId == personId && pa.AliasPersonId == null, ct);

            if (personAlias == null)
            {
                return new MarkAttendanceResultDto(
                    Success: false,
                    ErrorMessage: "Person alias not found");
            }

            // Check if attendance already exists
            var existingAttendance = await context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.OccurrenceId == occurrenceId &&
                    a.PersonAliasId == personAlias.Id, ct);

            // Calculate if this is first time attendance
            var isFirstTime = false;
            if (occurrence.GroupId.HasValue)
            {
                isFirstTime = await IsFirstTimeAttendanceAsync(personId, occurrence.GroupId.Value, ct);
            }

            var presentDateTime = DateTime.UtcNow;

            if (existingAttendance != null)
            {
                // Update existing attendance
                existingAttendance.DidAttend = true;
                existingAttendance.PresentDateTime = presentDateTime;
                existingAttendance.IsFirstTime = isFirstTime;
                if (!string.IsNullOrEmpty(note))
                {
                    existingAttendance.Note = note;
                }
                await context.SaveChangesAsync(ct);

                stopwatch.Stop();
                LogPerformance(stopwatch.ElapsedMilliseconds, "MarkAttended (update)", personId);

                return new MarkAttendanceResultDto(
                    Success: true,
                    AttendanceIdKey: existingAttendance.IdKey,
                    IsFirstTime: isFirstTime,
                    PresentDateTime: presentDateTime);
            }
            else
            {
                // Create new attendance
                var attendance = new Attendance
                {
                    OccurrenceId = occurrenceId,
                    PersonAliasId = personAlias.Id,
                    StartDateTime = presentDateTime,
                    DidAttend = true,
                    PresentDateTime = presentDateTime,
                    IsFirstTime = isFirstTime,
                    Note = note
                };

                context.Attendances.Add(attendance);
                await context.SaveChangesAsync(ct);

                stopwatch.Stop();
                LogPerformance(stopwatch.ElapsedMilliseconds, "MarkAttended (create)", personId);

                return new MarkAttendanceResultDto(
                    Success: true,
                    AttendanceIdKey: attendance.IdKey,
                    IsFirstTime: isFirstTime,
                    PresentDateTime: presentDateTime);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking attendance for person {PersonIdKey} at occurrence {OccurrenceIdKey}",
                personIdKey, occurrenceIdKey);

            return new MarkAttendanceResultDto(
                Success: false,
                ErrorMessage: "An unexpected error occurred while marking attendance");
        }
    }

    public async Task<BulkMarkAttendanceResultDto> MarkFamilyAttendedAsync(
        string occurrenceIdKey,
        string familyIdKey,
        string? note = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Decode family ID
            if (!IdKeyHelper.TryDecode(familyIdKey, out int familyId))
            {
                return new BulkMarkAttendanceResultDto(
                    Results: new List<MarkAttendanceResultDto>
                    {
                        new(Success: false, ErrorMessage: "Invalid family ID")
                    },
                    SuccessCount: 0,
                    FailureCount: 1,
                    AllSucceeded: false);
            }

            // Get all family members
            var familyMembers = await context.FamilyMembers
                .AsNoTracking()
                .Where(fm => fm.FamilyId == familyId)
                .Select(fm => fm.Person.IdKey)
                .ToListAsync(ct);

            if (familyMembers.Count == 0)
            {
                return new BulkMarkAttendanceResultDto(
                    Results: new List<MarkAttendanceResultDto>
                    {
                        new(Success: false, ErrorMessage: "No family members found")
                    },
                    SuccessCount: 0,
                    FailureCount: 1,
                    AllSucceeded: false);
            }

            // Mark each family member
            var results = new List<MarkAttendanceResultDto>();
            foreach (var memberIdKey in familyMembers)
            {
                var result = await MarkAttendedAsync(occurrenceIdKey, memberIdKey, note, ct);
                results.Add(result);
            }

            stopwatch.Stop();

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            LogPerformance(stopwatch.ElapsedMilliseconds, "MarkFamilyAttended", familyId, results.Count);

            return new BulkMarkAttendanceResultDto(
                Results: results,
                SuccessCount: successCount,
                FailureCount: failureCount,
                AllSucceeded: failureCount == 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking family attendance for family {FamilyIdKey} at occurrence {OccurrenceIdKey}",
                familyIdKey, occurrenceIdKey);

            return new BulkMarkAttendanceResultDto(
                Results: new List<MarkAttendanceResultDto>
                {
                    new(Success: false, ErrorMessage: "An unexpected error occurred")
                },
                SuccessCount: 0,
                FailureCount: 1,
                AllSucceeded: false);
        }
    }

    public async Task<bool> UnmarkAttendedAsync(
        string occurrenceIdKey,
        string personIdKey,
        CancellationToken ct = default)
    {
        try
        {
            // Decode IDs
            if (!IdKeyHelper.TryDecode(occurrenceIdKey, out int occurrenceId))
            {
                logger.LogWarning("Invalid occurrence ID key: {IdKey}", occurrenceIdKey);
                return false;
            }

            if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
            {
                logger.LogWarning("Invalid person ID key: {IdKey}", personIdKey);
                return false;
            }

            // Get person's aliases
            var personAliasIds = await context.PersonAliases
                .AsNoTracking()
                .Where(pa => pa.PersonId == personId)
                .Select(pa => pa.Id)
                .ToListAsync(ct);

            // Find attendance record
            var attendance = await context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.OccurrenceId == occurrenceId &&
                    a.PersonAliasId.HasValue &&
                    personAliasIds.Contains(a.PersonAliasId.Value), ct);

            if (attendance == null)
            {
                logger.LogWarning("Attendance not found for person {PersonIdKey} at occurrence {OccurrenceIdKey}",
                    personIdKey, occurrenceIdKey);
                return false;
            }

            // Set DidAttend to false and clear present timestamp
            attendance.DidAttend = false;
            attendance.PresentDateTime = null;
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Unmarked attendance for person {PersonId} at occurrence {OccurrenceId}",
                personId, occurrenceId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unmarking attendance for person {PersonIdKey} at occurrence {OccurrenceIdKey}",
                personIdKey, occurrenceIdKey);
            return false;
        }
    }

    public async Task<IReadOnlyList<OccurrenceRosterEntryDto>> GetOccurrenceRosterAsync(
        string occurrenceIdKey,
        CancellationToken ct = default)
    {
        try
        {
            // Decode occurrence ID
            if (!IdKeyHelper.TryDecode(occurrenceIdKey, out int occurrenceId))
            {
                return Array.Empty<OccurrenceRosterEntryDto>();
            }

            // Get occurrence with group
            var occurrence = await context.AttendanceOccurrences
                .AsNoTracking()
                .Include(o => o.Group)
                .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct);

            if (occurrence?.Group == null)
            {
                return Array.Empty<OccurrenceRosterEntryDto>();
            }

            // Get all group members
            var groupMembers = await context.GroupMembers
                .AsNoTracking()
                .Where(gm => gm.GroupId == occurrence.Group.Id)
                .Include(gm => gm.Person)
                .ToListAsync(ct);

            // Get all attendance records for this occurrence
            var attendances = await context.Attendances
                .AsNoTracking()
                .Where(a => a.OccurrenceId == occurrenceId)
                .Include(a => a.PersonAlias)
                .ToListAsync(ct);

            // Batch load PersonAliases to avoid N+1 query
            var personIds = groupMembers.Select(gm => gm.PersonId).ToList();
            var personAliases = await context.PersonAliases
                .AsNoTracking()
                .Where(pa => pa.PersonId.HasValue && personIds.Contains(pa.PersonId.Value) && pa.AliasPersonId == null)
                .ToListAsync(ct);
            var personAliasDict = personAliases.Where(pa => pa.PersonId.HasValue).ToDictionary(pa => pa.PersonId!.Value);

            // Build roster with attendance status
            var roster = new List<OccurrenceRosterEntryDto>();
            foreach (var groupMember in groupMembers)
            {
                var person = groupMember.Person;
                if (person == null)
                {
                    continue;
                }
                // Use pre-loaded PersonAlias from dictionary
                if (!personAliasDict.TryGetValue(person.Id, out var personAlias))
                {
                    continue;
                }
                var attendance = attendances.FirstOrDefault(a => a.PersonAliasId == personAlias.Id);

                roster.Add(new OccurrenceRosterEntryDto(
                    PersonIdKey: person.IdKey,
                    FullName: person.FullName,
                    FirstName: person.FirstName,
                    LastName: person.LastName,
                    NickName: person.NickName,
                    Age: CalculateAge(person.BirthDate),
                    PhotoUrl: null,
                    IsAttending: attendance?.DidAttend ?? false,
                    AttendanceIdKey: attendance?.IdKey,
                    PresentDateTime: attendance?.PresentDateTime,
                    IsFirstTime: attendance?.IsFirstTime ?? false,
                    Note: attendance?.Note));
            }

            return roster.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting occurrence roster for occurrence {OccurrenceIdKey}", occurrenceIdKey);
            return Array.Empty<OccurrenceRosterEntryDto>();
        }
    }

    public async Task<IReadOnlyList<FamilyRosterGroupDto>> GetFamilyGroupedRosterAsync(
        string occurrenceIdKey,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        try
        {
            // Decode occurrence ID
            if (!IdKeyHelper.TryDecode(occurrenceIdKey, out int occurrenceId))
            {
                return Array.Empty<FamilyRosterGroupDto>();
            }

            // Get occurrence with group
            var occurrence = await context.AttendanceOccurrences
                .AsNoTracking()
                .Include(o => o.Group)
                .FirstOrDefaultAsync(o => o.Id == occurrenceId, ct);

            if (occurrence?.Group == null)
            {
                return Array.Empty<FamilyRosterGroupDto>();
            }

            // Get all group members with their families
            var groupMembersQuery = context.GroupMembers
                .AsNoTracking()
                .Where(gm => gm.GroupId == occurrence.Group.Id)
                .Include(gm => gm.Person);

            var groupMembers = await groupMembersQuery.ToListAsync(ct);
            var personIds = groupMembers.Select(gm => gm.PersonId).Distinct().ToList();

            // Get family memberships for these people
            var familyMemberships = await context.FamilyMembers
                .AsNoTracking()
                .Where(fm => personIds.Contains(fm.PersonId))
                .Include(fm => fm.Family)
                .Include(fm => fm.Person)
                .ToListAsync(ct);

            // Get all attendance records for this occurrence
            var attendances = await context.Attendances
                .AsNoTracking()
                .Where(a => a.OccurrenceId == occurrenceId)
                .Include(a => a.PersonAlias)
                .ToListAsync(ct);

            // Batch load PersonAliases to avoid N+1 query
            var allPersonIds = familyMemberships.Select(fm => fm.PersonId).Distinct().ToList();
            var allPersonAliases = await context.PersonAliases
                .AsNoTracking()
                .Where(pa => pa.PersonId.HasValue && allPersonIds.Contains(pa.PersonId.Value) && pa.AliasPersonId == null)
                .ToListAsync(ct);
            var personAliasDictForFamily = allPersonAliases.Where(pa => pa.PersonId.HasValue).ToDictionary(pa => pa.PersonId!.Value);

            // Group by family
            var familyGroups = familyMemberships
                .GroupBy(fm => fm.FamilyId)
                .Select(g => new
                {
                    FamilyId = g.Key,
                    Family = g.First().Family,
                    Members = g.Select(fm => fm.Person).ToList()
                })
                .ToList();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLowerInvariant();
                familyGroups = familyGroups
                    .Where(fg =>
                        fg.Family.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                        fg.Members.Any(m =>
                            m != null &&
                            (m.FullName.ToLowerInvariant().Contains(lowerSearchTerm) ||
                            m.FirstName.ToLowerInvariant().Contains(lowerSearchTerm) ||
                            m.LastName.ToLowerInvariant().Contains(lowerSearchTerm))))
                    .ToList();
            }

            // Build family roster groups
            var rosterGroups = new List<FamilyRosterGroupDto>();
            foreach (var familyGroup in familyGroups)
            {
                var memberEntries = new List<OccurrenceRosterEntryDto>();
                foreach (var person in familyGroup.Members)
                {
                    if (person == null)
                    {
                        continue;
                    }
                    // Use pre-loaded PersonAlias from dictionary
                    if (!personAliasDictForFamily.TryGetValue(person.Id, out var personAlias))
                    {
                        continue;
                    }

                    var attendance = attendances.FirstOrDefault(a => a.PersonAliasId == personAlias.Id);

                    memberEntries.Add(new OccurrenceRosterEntryDto(
                        PersonIdKey: person.IdKey,
                        FullName: person.FullName,
                        FirstName: person.FirstName,
                        LastName: person.LastName,
                        NickName: person.NickName,
                        Age: CalculateAge(person.BirthDate),
                        PhotoUrl: null,
                        IsAttending: attendance?.DidAttend ?? false,
                        AttendanceIdKey: attendance?.IdKey,
                        PresentDateTime: attendance?.PresentDateTime,
                        IsFirstTime: attendance?.IsFirstTime ?? false,
                        Note: attendance?.Note));
                }

                var attendingCount = memberEntries.Count(m => m.IsAttending);

                rosterGroups.Add(new FamilyRosterGroupDto(
                    FamilyIdKey: familyGroup.Family.IdKey,
                    FamilyName: familyGroup.Family.Name,
                    Members: memberEntries.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToList(),
                    AttendingCount: attendingCount,
                    TotalCount: memberEntries.Count));
            }

            return rosterGroups.OrderBy(fg => fg.FamilyName).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting family-grouped roster for occurrence {OccurrenceIdKey}", occurrenceIdKey);
            return Array.Empty<FamilyRosterGroupDto>();
        }
    }

    public async Task<BulkMarkAttendanceResultDto> BulkMarkAttendedAsync(
        string occurrenceIdKey,
        string[] personIdKeys,
        string? note = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var results = new List<MarkAttendanceResultDto>();

            // Mark each person
            foreach (var personIdKey in personIdKeys)
            {
                var result = await MarkAttendedAsync(occurrenceIdKey, personIdKey, note, ct);
                results.Add(result);
            }

            stopwatch.Stop();

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            LogPerformance(stopwatch.ElapsedMilliseconds, "BulkMarkAttended", 0, results.Count);

            return new BulkMarkAttendanceResultDto(
                Results: results,
                SuccessCount: successCount,
                FailureCount: failureCount,
                AllSucceeded: failureCount == 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk marking attendance for occurrence {OccurrenceIdKey}", occurrenceIdKey);

            return new BulkMarkAttendanceResultDto(
                Results: new List<MarkAttendanceResultDto>
                {
                    new(Success: false, ErrorMessage: "An unexpected error occurred")
                },
                SuccessCount: 0,
                FailureCount: 1,
                AllSucceeded: false);
        }
    }

    // Private helper methods

    private async Task<bool> IsFirstTimeAttendanceAsync(int personId, int groupId, CancellationToken ct)
    {
        // Get person alias IDs for this person
        var personAliasIds = await context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId == personId)
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        // Check if person has any previous attendance at this group
        var hasPreviousAttendance = await context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.GroupId == groupId)
            .SelectMany(o => o.Attendances)
            .Where(a => a.PersonAliasId.HasValue)
            .AnyAsync(a => personAliasIds.Contains(a.PersonAliasId!.Value), ct);

        return !hasPreviousAttendance;
    }

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (!birthDate.HasValue)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;

        if (birthDate.Value > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private void LogPerformance(long elapsedMs, string operation, int entityId, int? count = null)
    {
        var target = operation.StartsWith("MarkAttended") && !operation.Contains("Family") && !operation.Contains("Bulk")
            ? 100
            : 500;

        if (elapsedMs > target)
        {
            logger.LogWarning(
                "{Operation} exceeded {Target}ms target: {Elapsed}ms for entity {EntityId}" +
                (count.HasValue ? " ({Count} items)" : ""),
                operation, target, elapsedMs, entityId, count);
        }
        else
        {
            logger.LogInformation(
                "{Operation} completed in {Elapsed}ms for entity {EntityId}" +
                (count.HasValue ? " ({Count} items)" : ""),
                operation, elapsedMs, entityId, count);
        }
    }
}
