using Koinon.Application.Constants;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for room roster operations.
/// Provides real-time roster views for teachers and volunteers to see who is in their classroom.
/// </summary>
public class RoomRosterService(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger<RoomRosterService> logger,
    IGradeCalculationService gradeCalculationService,
    ICheckinAttendanceService attendanceService)
    : AuthorizedCheckinService(context, userContext, logger), IRoomRosterService
{
    private readonly IGradeCalculationService _gradeCalculationService = gradeCalculationService;
    private readonly ICheckinAttendanceService _attendanceService = attendanceService;

    public async Task<RoomRosterDto> GetRoomRosterAsync(string locationIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            Logger.LogWarning("Invalid location IdKey: {LocationIdKey}", locationIdKey);
            return new RoomRosterDto(
                LocationIdKey: locationIdKey,
                LocationName: "Invalid Location",
                Children: new List<RosterChildDto>(),
                TotalCount: 0,
                Capacity: null,
                GeneratedAt: DateTime.UtcNow,
                IsAtCapacity: false,
                IsNearCapacity: false);
        }

        // Authorization check - throws UnauthorizedAccessException if denied
        // Let it propagate to the global exception handler which returns 403
        AuthorizeLocationAccess(locationId, nameof(GetRoomRosterAsync));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get location details
        var location = await Context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == locationId, ct);

        if (location == null)
        {
            Logger.LogWarning("Location not found: {LocationId}", locationId);
            return new RoomRosterDto(
                LocationIdKey: locationIdKey,
                LocationName: "Location Not Found",
                Children: new List<RosterChildDto>(),
                TotalCount: 0,
                Capacity: null,
                GeneratedAt: DateTime.UtcNow,
                IsAtCapacity: false,
                IsNearCapacity: false);
        }

        // Get current attendance for the location
        var attendances = await Context.Attendances
            .AsNoTracking()
            .Include(a => a.AttendanceCode)
            .Include(a => a.Occurrence)
            .Where(a => a.Occurrence != null
                && a.Occurrence.GroupId == locationId
                && a.Occurrence.OccurrenceDate == today
                && a.EndDateTime == null) // Only currently checked in
            .OrderBy(a => a.StartDateTime)
            .ToListAsync(ct);

        // Get all person alias IDs
        var personAliasIds = attendances
            .Where(a => a.PersonAliasId.HasValue)
            .Select(a => a.PersonAliasId!.Value)
            .Distinct()
            .ToList();

        // Batch load people with their families and phone numbers
        var peopleData = await Context.PersonAliases
            .AsNoTracking()
            .Where(pa => personAliasIds.Contains(pa.Id))
            .Join(Context.People,
                pa => pa.PersonId,
                p => p.Id,
                (pa, p) => new { PersonAliasId = pa.Id, Person = p })
            .ToDictionaryAsync(x => x.PersonAliasId, x => x.Person, ct);

        // Get person IDs to load families
        var personIds = peopleData.Values.Select(p => p.Id).Distinct().ToList();

        // Get family memberships for all children
        var familyMemberships = await Context.GroupMembers
            .AsNoTracking()
            .Where(gm => personIds.Contains(gm.PersonId)
                && gm.Group != null
                && gm.Group.GroupType != null
                && gm.Group.GroupType.Name == "Family")
            .Select(gm => new { gm.PersonId, FamilyId = gm.GroupId })
            .ToListAsync(ct);

        var familyIdsByPerson = familyMemberships
            .GroupBy(fm => fm.PersonId)
            .Select(g => new { PersonId = g.Key, FamilyId = g.FirstOrDefault()?.FamilyId })
            .Where(x => x.FamilyId.HasValue)
            .ToDictionary(x => x.PersonId, x => x.FamilyId!.Value);

        // Get all parent info for these families
        var familyIds = familyIdsByPerson.Values.Distinct().ToList();

        var parentInfo = await Context.GroupMembers
            .AsNoTracking()
            .Where(gm => familyIds.Contains(gm.GroupId)
                && gm.Person != null
                && gm.GroupRole != null
                && gm.GroupRole.IsLeader) // Parents are leaders in family groups
            .Select(gm => new
            {
                FamilyId = gm.GroupId,
                ParentName = gm.Person!.FirstName + " " + gm.Person.LastName,
                MobilePhone = gm.Person.PhoneNumbers
                    .Where(pn => pn.NumberTypeValue != null && pn.NumberTypeValue.Value == "Mobile")
                    .Select(pn => pn.Number)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var parentsByFamily = parentInfo
            .GroupBy(pi => pi.FamilyId)
            .Select(g => new { FamilyId = g.Key, Parent = g.FirstOrDefault() })
            .Where(x => x.Parent != null)
            .ToDictionary(x => x.FamilyId, x => x.Parent!);

        // Build roster
        var rosterChildren = new List<RosterChildDto>();

        foreach (var attendance in attendances)
        {
            if (!attendance.PersonAliasId.HasValue || !peopleData.TryGetValue(attendance.PersonAliasId.Value, out var person))
            {
                Logger.LogWarning(
                    "Attendance {AttendanceId} has missing PersonAlias data - skipping",
                    attendance.Id);
                continue;
            }

            // Get parent info if available
            string? parentName = null;
            string? parentPhone = null;

            if (familyIdsByPerson.TryGetValue(person.Id, out int familyId))
            {
                if (parentsByFamily.TryGetValue(familyId, out var parent))
                {
                    parentName = parent.ParentName;
                    parentPhone = parent.MobilePhone;
                }
            }

            // Calculate grade
            var grade = _gradeCalculationService.CalculateGrade(person.GraduationYear);
            string? gradeDisplay = grade.HasValue ? FormatGrade(grade.Value) : null;

            // Calculate age
            int? age = CalculateAge(person.BirthDate);

            rosterChildren.Add(new RosterChildDto(
                AttendanceIdKey: attendance.IdKey,
                PersonIdKey: person.IdKey,
                FullName: person.FullName,
                FirstName: person.FirstName,
                LastName: person.LastName,
                NickName: person.NickName,
                PhotoUrl: person.Photo != null ? ApiPaths.GetFileUrl(person.Photo.IdKey) : null,
                Age: age,
                Grade: gradeDisplay,
                Allergies: person.Allergies,
                HasCriticalAllergies: person.HasCriticalAllergies,
                SpecialNeeds: person.SpecialNeeds,
                SecurityCode: attendance.AttendanceCode?.Code,
                CheckInTime: attendance.StartDateTime,
                ParentName: parentName,
                ParentMobilePhone: parentPhone,
                IsFirstTime: attendance.IsFirstTime));
        }

        // Calculate capacity metrics
        var totalCount = rosterChildren.Count;
        var capacity = location.GroupCapacity;
        var isAtCapacity = capacity.HasValue && totalCount >= capacity.Value;
        var isNearCapacity = capacity.HasValue && totalCount >= (capacity.Value * 0.8);

        return new RoomRosterDto(
            LocationIdKey: locationIdKey,
            LocationName: location.Name,
            Children: rosterChildren,
            TotalCount: totalCount,
            Capacity: capacity,
            GeneratedAt: DateTime.UtcNow,
            IsAtCapacity: isAtCapacity,
            IsNearCapacity: isNearCapacity);
    }

    public async Task<IReadOnlyList<RoomRosterDto>> GetMultipleRoomRostersAsync(
        IEnumerable<string> locationIdKeys,
        CancellationToken ct = default)
    {
        var tasks = locationIdKeys.Select(idKey => GetRoomRosterAsync(idKey, ct));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<bool> CheckOutFromRosterAsync(string attendanceIdKey, CancellationToken ct = default)
    {
        // Delegate to the attendance service which has all the authorization logic
        return await _attendanceService.CheckOutAsync(attendanceIdKey, ct);
    }

    // Private helper methods

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

    private static string FormatGrade(int grade)
    {
        return grade switch
        {
            < 0 => "Pre-K",
            0 => "Kindergarten",
            > 12 => "Graduate",
            _ => $"{grade}th Grade"
        };
    }
}
