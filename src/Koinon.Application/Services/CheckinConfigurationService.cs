using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for check-in configuration operations.
/// Manages check-in area configurations, schedules, and location capacity tracking.
/// </summary>
public class CheckinConfigurationService(
    IApplicationDbContext context,
    IUserContext userContext,
    IGradeCalculationService gradeCalculationService,
    ILogger<CheckinConfigurationService> logger)
    : AuthorizedCheckinService(context, userContext, logger), ICheckinConfigurationService
{
    private readonly IGradeCalculationService _gradeCalculationService = gradeCalculationService;
    public async Task<CheckinConfigurationDto?> GetConfigurationByCampusAsync(
        string campusIdKey,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetConfigurationByCampusAsync));

        if (!IdKeyHelper.TryDecode(campusIdKey, out int campusId))
        {
            return null;
        }

        var campus = await Context.Campuses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campusId && c.IsActive, ct);

        if (campus is null)
        {
            return null;
        }

        var currentTime = DateTime.UtcNow;

        // Get active check-in areas for this campus
        var areas = await GetActiveAreasAsync(campusIdKey, currentTime, ct);

        // Get active schedules for this campus
        var schedules = await GetActiveSchedulesAsync(campusIdKey, currentTime, ct);

        Logger.LogInformation(
            "Retrieved check-in configuration for campus {CampusId}: {AreaCount} areas, {ScheduleCount} schedules",
            campusId, areas.Count, schedules.Count);

        return new CheckinConfigurationDto
        {
            Campus = new CampusSummaryDto
            {
                IdKey = campus.IdKey,
                Name = campus.Name,
                ShortCode = campus.ShortCode
            },
            Areas = areas,
            ActiveSchedules = schedules,
            ServerTime = currentTime
        };
    }

    public Task<CheckinConfigurationDto?> GetConfigurationByKioskAsync(
        string deviceIdKey,
        CancellationToken ct = default)
    {
        // Device entity and kiosk-specific configuration not yet implemented
        // Returns null until Device entity is added with Campus linking
        return Task.FromResult<CheckinConfigurationDto?>(null);
    }

    public async Task<IReadOnlyList<CheckinAreaDto>> GetActiveAreasAsync(
        string campusIdKey,
        DateTime? currentTime = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetActiveAreasAsync));

        if (!IdKeyHelper.TryDecode(campusIdKey, out int campusId))
        {
            return Array.Empty<CheckinAreaDto>();
        }

        currentTime ??= DateTime.UtcNow;
        var currentDate = DateOnly.FromDateTime(currentTime.Value);

        // Get all groups that are check-in areas for this campus
        // Check-in areas are top-level groups where GroupType.TakesAttendance = true
        // (groups without a parent or whose parent doesn't take attendance)
        var areas = await Context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
                .ThenInclude(gt => gt!.Roles)
            .Include(g => g.GroupSchedules)
                .ThenInclude(gs => gs.Schedule)
            .Include(g => g.Schedule)  // Keep for backwards compatibility
            .Include(g => g.Campus)
            .Include(g => g.ParentGroup)
                .ThenInclude(pg => pg!.GroupType)
            .Where(g => g.CampusId == campusId
                && g.IsActive
                && !g.IsArchived
                && g.GroupType!.TakesAttendance
                && (!g.ParentGroupId.HasValue || !g.ParentGroup!.GroupType!.TakesAttendance))
            .OrderBy(g => g.Order)
            .ThenBy(g => g.Name)
            .ToListAsync(ct);

        var areaIds = areas.Select(a => a.Id).ToList();

        // Get locations for each area (child groups that are rooms/classrooms)
        var locations = await Context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Where(g => g.ParentGroupId.HasValue
                && areaIds.Contains(g.ParentGroupId.Value)
                && g.IsActive
                && !g.IsArchived)
            .ToListAsync(ct);

        var locationGroupIds = locations.Select(l => l.Id).ToList();

        // Get current attendance counts for capacity tracking
        // Use SelectMany + GroupBy pattern to avoid N+1 queries
        var attendanceCounts = await Context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.OccurrenceDate == currentDate
                && o.GroupId.HasValue
                && locationGroupIds.Contains(o.GroupId.Value))
            .SelectMany(o => o.Attendances
                .Where(a => a.EndDateTime == null) // Only count people who haven't checked out
                .Select(a => o.GroupId!.Value))
            .GroupBy(gid => gid)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        var result = new List<CheckinAreaDto>();

        foreach (var area in areas)
        {
            // Get locations for this area
            var areaLocations = locations
                .Where(l => l.ParentGroupId == area.Id)
                .Select(l => MapToCheckinLocationDto(l, attendanceCounts, currentDate))
                .ToList();

            // Calculate overall capacity status for the area
            var capacityStatus = CalculateAreaCapacityStatus(areaLocations);

            // Check if area is currently open based on schedule (use GroupSchedules first, fall back to Schedule)
            ScheduleDto? scheduleDto = null;
            if (area.GroupSchedules?.Any() == true)
            {
                // Find first active schedule in the check-in window
                var activeGroupSchedule = area.GroupSchedules
                    .Where(gs => gs.Schedule != null && gs.Schedule.IsActive)
                    .OrderBy(gs => gs.Order)
                    .FirstOrDefault(gs => IsScheduleCheckinActive(gs.Schedule!, currentTime.Value));

                if (activeGroupSchedule?.Schedule != null)
                {
                    scheduleDto = MapToScheduleDto(activeGroupSchedule.Schedule, currentTime.Value);
                }
            }
            // Fall back to legacy single Schedule
            else if (area.Schedule != null)
            {
                scheduleDto = MapToScheduleDto(area.Schedule, currentTime.Value);
            }

            result.Add(new CheckinAreaDto
            {
                IdKey = area.IdKey,
                Guid = area.Guid,
                Name = area.Name,
                Description = area.Description,
                GroupType = new GroupTypeDto
                {
                    IdKey = area.GroupType!.IdKey,
                    Guid = area.GroupType.Guid,
                    Name = area.GroupType.Name,
                    Description = area.GroupType.Description,
                    IsFamilyGroupType = area.GroupType.IsFamilyGroupType,
                    AllowMultipleLocations = area.GroupType.AllowMultipleLocations,
                    Roles = area.GroupType.Roles
                        .Select(r => new GroupTypeRoleDto
                        {
                            IdKey = r.IdKey,
                            Name = r.Name,
                            IsLeader = r.IsLeader
                        })
                        .ToList()
                },
                Locations = areaLocations,
                Schedule = scheduleDto,
                IsActive = area.IsActive,
                CapacityStatus = capacityStatus,
                MinAgeMonths = area.MinAgeMonths,
                MaxAgeMonths = area.MaxAgeMonths,
                MinGrade = area.MinGrade,
                MaxGrade = area.MaxGrade
            });
        }

        Logger.LogInformation(
            "Retrieved {Count} active check-in areas for campus {CampusId}",
            result.Count, campusId);

        return result;
    }

    public async Task<CheckinAreaDto?> GetAreaByIdKeyAsync(
        string areaIdKey,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetAreaByIdKeyAsync));

        if (!IdKeyHelper.TryDecode(areaIdKey, out int areaId))
        {
            return null;
        }

        var area = await Context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
                .ThenInclude(gt => gt!.Roles)
            .Include(g => g.GroupSchedules)
                .ThenInclude(gs => gs.Schedule)
            .Include(g => g.Schedule)  // Keep for backwards compatibility
            .Include(g => g.Campus)
            .FirstOrDefaultAsync(g => g.Id == areaId
                && g.GroupType!.TakesAttendance
                && !g.IsArchived, ct);

        if (area is null)
        {
            return null;
        }

        var currentTime = DateTime.UtcNow;
        var currentDate = DateOnly.FromDateTime(currentTime);

        // Get locations for this area
        var locations = await Context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Where(g => g.ParentGroupId == areaId
                && g.IsActive
                && !g.IsArchived)
            .ToListAsync(ct);

        var locationGroupIds = locations.Select(l => l.Id).ToList();

        // Get current attendance counts
        // Use SelectMany + GroupBy pattern to avoid N+1 queries
        var attendanceCounts = await Context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.OccurrenceDate == currentDate
                && o.GroupId.HasValue
                && locationGroupIds.Contains(o.GroupId.Value))
            .SelectMany(o => o.Attendances
                .Where(a => a.EndDateTime == null)
                .Select(a => o.GroupId!.Value))
            .GroupBy(gid => gid)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        var locationDtos = locations
            .Select(l => MapToCheckinLocationDto(l, attendanceCounts, currentDate))
            .ToList();

        var capacityStatus = CalculateAreaCapacityStatus(locationDtos);

        // Check if area is currently open based on schedule (use GroupSchedules first, fall back to Schedule)
        ScheduleDto? scheduleDto = null;
        if (area.GroupSchedules?.Any() == true)
        {
            // Find first active schedule in the check-in window
            var activeGroupSchedule = area.GroupSchedules
                .Where(gs => gs.Schedule != null && gs.Schedule.IsActive)
                .OrderBy(gs => gs.Order)
                .FirstOrDefault(gs => IsScheduleCheckinActive(gs.Schedule!, currentTime));

            if (activeGroupSchedule?.Schedule != null)
            {
                scheduleDto = MapToScheduleDto(activeGroupSchedule.Schedule, currentTime);
            }
        }
        // Fall back to legacy single Schedule
        else if (area.Schedule != null)
        {
            scheduleDto = MapToScheduleDto(area.Schedule, currentTime);
        }

        return new CheckinAreaDto
        {
            IdKey = area.IdKey,
            Guid = area.Guid,
            Name = area.Name,
            Description = area.Description,
            GroupType = new GroupTypeDto
            {
                IdKey = area.GroupType!.IdKey,
                Guid = area.GroupType.Guid,
                Name = area.GroupType.Name,
                Description = area.GroupType.Description,
                IsFamilyGroupType = area.GroupType.IsFamilyGroupType,
                AllowMultipleLocations = area.GroupType.AllowMultipleLocations,
                Roles = area.GroupType.Roles
                    .Select(r => new GroupTypeRoleDto
                    {
                        IdKey = r.IdKey,
                        Name = r.Name,
                        IsLeader = r.IsLeader
                    })
                    .ToList()
            },
            Locations = locationDtos,
            Schedule = scheduleDto,
            IsActive = area.IsActive,
            CapacityStatus = capacityStatus,
            MinAgeMonths = area.MinAgeMonths,
            MaxAgeMonths = area.MaxAgeMonths,
            MinGrade = area.MinGrade,
            MaxGrade = area.MaxGrade
        };
    }

    public async Task<CheckinLocationDto?> GetLocationCapacityAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetLocationCapacityAsync));

        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            return null;
        }

        // Note: For MVP, locations are represented as child groups
        // In the future, this might use the Location entity directly
        var location = await Context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == locationId && !g.IsArchived, ct);

        if (location is null)
        {
            return null;
        }

        occurrenceDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Get current attendance count using efficient subquery
        var currentCount = await Context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.OccurrenceDate == occurrenceDate.Value && o.GroupId == locationId)
            .Select(o => o.Attendances.Count(a => a.EndDateTime == null))
            .FirstOrDefaultAsync(ct);

        var attendanceCounts = new Dictionary<int, int> { { locationId, currentCount } };

        return MapToCheckinLocationDto(location, attendanceCounts, occurrenceDate.Value);
    }

    public async Task<IReadOnlyList<ScheduleDto>> GetActiveSchedulesAsync(
        string campusIdKey,
        DateTime? currentTime = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetActiveSchedulesAsync));

        if (!IdKeyHelper.TryDecode(campusIdKey, out int campusId))
        {
            return Array.Empty<ScheduleDto>();
        }

        currentTime ??= DateTime.UtcNow;
        var currentDate = DateOnly.FromDateTime(currentTime.Value);

        // Get all schedules for check-in areas at this campus (via GroupSchedules or legacy Schedule FK)
        var schedules = await Context.Schedules
            .AsNoTracking()
            .Where(s => s.IsActive
                && (
                    // New: via GroupSchedules
                    s.GroupSchedules.Any(gs => gs.Group!.CampusId == campusId
                        && gs.Group.GroupType!.TakesAttendance
                        && gs.Group.IsActive
                        && !gs.Group.IsArchived)
                    ||
                    // Legacy: via direct ScheduleId FK
                    s.Groups.Any(g => g.CampusId == campusId
                        && g.GroupType!.TakesAttendance
                        && g.IsActive
                        && !g.IsArchived)
                )
                && (s.EffectiveStartDate == null || s.EffectiveStartDate <= currentDate)
                && (s.EffectiveEndDate == null || s.EffectiveEndDate >= currentDate))
            .OrderBy(s => s.Order)
            .ThenBy(s => s.WeeklyTimeOfDay)
            .ToListAsync(ct);

        return schedules
            .Select(s => MapToScheduleDto(s, currentTime.Value))
            .Where(s => s.IsCheckinActive) // Only return schedules that are currently open for check-in
            .ToList();
    }

    public async Task<bool> IsCheckinOpenAsync(
        string scheduleIdKey,
        DateTime? currentTime = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(IsCheckinOpenAsync));

        if (!IdKeyHelper.TryDecode(scheduleIdKey, out int scheduleId))
        {
            return false;
        }

        currentTime ??= DateTime.UtcNow;

        var schedule = await Context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.IsActive, ct);

        if (schedule is null)
        {
            return false;
        }

        return IsScheduleCheckinActive(schedule, currentTime.Value);
    }

    // Private helper methods

    private static CheckinLocationDto MapToCheckinLocationDto(
        Domain.Entities.Group locationGroup,
        Dictionary<int, int> attendanceCounts,
        DateOnly currentDate)
    {
        var currentCount = attendanceCounts.ContainsKey(locationGroup.Id)
            ? attendanceCounts[locationGroup.Id]
            : 0;

        var capacityStatus = CalculateCapacityStatus(
            currentCount,
            locationGroup.GroupCapacity, // Using GroupCapacity as soft threshold
            null); // No separate hard capacity for MVP

        return new CheckinLocationDto
        {
            IdKey = locationGroup.IdKey,
            Name = locationGroup.Name,
            FullPath = locationGroup.Name, // For MVP, just use name. Future: build full path from parent hierarchy
            SoftCapacity = locationGroup.GroupCapacity,
            HardCapacity = null,
            CurrentCount = currentCount,
            CapacityStatus = capacityStatus,
            IsActive = locationGroup.IsActive,
            PrinterDeviceIdKey = null // For MVP, printer assignment not yet implemented
        };
    }

    private static ScheduleDto MapToScheduleDto(Domain.Entities.Schedule schedule, DateTime currentTime)
    {
        var isCheckinActive = IsScheduleCheckinActive(schedule, currentTime);

        DateTime? checkinStartTime = null;
        DateTime? checkinEndTime = null;

        if (schedule.WeeklyDayOfWeek.HasValue && schedule.WeeklyTimeOfDay.HasValue)
        {
            // Calculate check-in window for weekly schedules
            var scheduledDateTime = GetNextScheduledDateTime(
                schedule.WeeklyDayOfWeek.Value,
                schedule.WeeklyTimeOfDay.Value,
                currentTime);

            var startOffset = schedule.CheckInStartOffsetMinutes ?? 60; // Default 60 minutes before
            var endOffset = schedule.CheckInEndOffsetMinutes ?? 30; // Default 30 minutes after

            checkinStartTime = scheduledDateTime.AddMinutes(-startOffset);
            checkinEndTime = scheduledDateTime.AddMinutes(endOffset);
        }

        return new ScheduleDto
        {
            IdKey = schedule.IdKey,
            Guid = schedule.Guid,
            Name = schedule.Name,
            Description = schedule.Description,
            WeeklyDayOfWeek = schedule.WeeklyDayOfWeek,
            WeeklyTimeOfDay = schedule.WeeklyTimeOfDay,
            CheckInStartOffsetMinutes = schedule.CheckInStartOffsetMinutes,
            CheckInEndOffsetMinutes = schedule.CheckInEndOffsetMinutes,
            IsActive = schedule.IsActive,
            IsCheckinActive = isCheckinActive,
            CheckinStartTime = checkinStartTime,
            CheckinEndTime = checkinEndTime
        };
    }

    private static bool IsScheduleCheckinActive(Domain.Entities.Schedule schedule, DateTime currentTime)
    {
        if (!schedule.IsActive)
        {
            return false;
        }

        // For weekly schedules, check if we're within the check-in window
        if (schedule.WeeklyDayOfWeek.HasValue && schedule.WeeklyTimeOfDay.HasValue)
        {
            var scheduledDateTime = GetNextScheduledDateTime(
                schedule.WeeklyDayOfWeek.Value,
                schedule.WeeklyTimeOfDay.Value,
                currentTime);

            var startOffset = schedule.CheckInStartOffsetMinutes ?? 60;
            var endOffset = schedule.CheckInEndOffsetMinutes ?? 30;

            var checkinStart = scheduledDateTime.AddMinutes(-startOffset);
            var checkinEnd = scheduledDateTime.AddMinutes(endOffset);

            return currentTime >= checkinStart && currentTime <= checkinEnd;
        }

        // For iCalendar schedules, would need to parse RRULE
        // For MVP, if no weekly schedule defined, assume always open if active
        return true;
    }

    private static DateTime GetNextScheduledDateTime(DayOfWeek dayOfWeek, TimeSpan timeOfDay, DateTime currentTime)
    {
        var currentDay = currentTime.DayOfWeek;
        var daysUntilScheduled = ((int)dayOfWeek - (int)currentDay + 7) % 7;

        // If it's the same day but time has passed, get next week's occurrence
        if (daysUntilScheduled == 0 && currentTime.TimeOfDay > timeOfDay)
        {
            daysUntilScheduled = 7;
        }

        var scheduledDate = currentTime.Date.AddDays(daysUntilScheduled);
        return scheduledDate.Add(timeOfDay);
    }

    private static CapacityStatus CalculateCapacityStatus(int currentCount, int? softCapacity, int? hardCapacity)
    {
        if (hardCapacity.HasValue && currentCount >= hardCapacity.Value)
        {
            return CapacityStatus.Full;
        }

        if (softCapacity.HasValue && currentCount >= softCapacity.Value)
        {
            return CapacityStatus.Warning;
        }

        return CapacityStatus.Available;
    }

    private static CapacityStatus CalculateAreaCapacityStatus(IReadOnlyList<CheckinLocationDto> locations)
    {
        if (locations.Count == 0)
        {
            return CapacityStatus.Available;
        }

        // If any location is full, the area is at warning level
        // If all locations are full, the area is full
        var fullLocations = locations.Count(l => l.CapacityStatus == CapacityStatus.Full);
        var warningLocations = locations.Count(l => l.CapacityStatus == CapacityStatus.Warning);

        if (fullLocations == locations.Count)
        {
            return CapacityStatus.Full;
        }

        if (fullLocations > 0 || warningLocations > 0)
        {
            return CapacityStatus.Warning;
        }

        return CapacityStatus.Available;
    }

    public IReadOnlyList<CheckinAreaDto> FilterAreasByPersonEligibility(
        IReadOnlyList<CheckinAreaDto> areas,
        DateOnly? personBirthDate,
        int? personGraduationYear,
        DateOnly? currentDate = null)
    {
        if (areas.Count == 0)
        {
            return areas;
        }

        currentDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Calculate person's age in months and grade
        var personAgeInMonths = _gradeCalculationService.CalculateAgeInMonths(personBirthDate, currentDate);
        var personGrade = _gradeCalculationService.CalculateGrade(personGraduationYear, currentDate);

        // Filter areas based on eligibility
        return areas
            .Where(area => IsAreaEligibleForPerson(area, personAgeInMonths, personGrade))
            .ToList();
    }

    private static bool IsAreaEligibleForPerson(
        CheckinAreaDto area,
        int? personAgeInMonths,
        int? personGrade)
    {
        // Age filtering
        // If person has no birth date, age filters pass (parent can choose)
        if (personAgeInMonths.HasValue)
        {
            // Check minimum age
            if (area.MinAgeMonths.HasValue && personAgeInMonths.Value < area.MinAgeMonths.Value)
            {
                return false;
            }

            // Check maximum age
            if (area.MaxAgeMonths.HasValue && personAgeInMonths.Value > area.MaxAgeMonths.Value)
            {
                return false;
            }
        }

        // Grade filtering
        // If person has no graduation year, grade filters pass (parent can choose)
        if (personGrade.HasValue)
        {
            // Check minimum grade
            if (area.MinGrade.HasValue && personGrade.Value < area.MinGrade.Value)
            {
                return false;
            }

            // Check maximum grade
            if (area.MaxGrade.HasValue && personGrade.Value > area.MaxGrade.Value)
            {
                return false;
            }
        }

        // If we've passed all filters, the area is eligible
        return true;
    }
}
