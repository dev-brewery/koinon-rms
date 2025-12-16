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

        // Get all active check-in areas for this campus
        var areas = await LoadAreasForCampusAsync(campusId, ct);
        var areaIds = areas.Select(a => a.Id).ToList();

        // Load locations and attendance data
        var locations = await LoadLocationsForAreasAsync(areaIds, ct);
        var attendanceCounts = await LoadAttendanceCountsAsync(locations, currentDate, ct);

        // Build area DTOs with capacity and schedule info
        var result = BuildAreaDtos(areas, locations, attendanceCounts, currentTime.Value, currentDate);

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

        var area = await LoadAreaByIdAsync(areaId, ct);
        if (area is null)
        {
            return null;
        }

        var currentTime = DateTime.UtcNow;
        var currentDate = DateOnly.FromDateTime(currentTime);

        // Load locations and attendance data for this specific area
        var locations = await LoadLocationsForAreasAsync(new[] { areaId }, ct);
        var attendanceCounts = await LoadAttendanceCountsAsync(locations, currentDate, ct);

        // Build single area DTO
        var areaDto = BuildAreaDto(area, locations, attendanceCounts, currentTime, currentDate);
        return areaDto;
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

    // Private helper methods - Data Loading

    /// <summary>
    /// Loads check-in areas for a campus with all required relationships.
    /// </summary>
    private async Task<List<Domain.Entities.Group>> LoadAreasForCampusAsync(
        int campusId,
        CancellationToken ct)
    {
        return await Context.Groups
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
    }

    /// <summary>
    /// Loads a single check-in area by ID with all required relationships.
    /// </summary>
    private async Task<Domain.Entities.Group?> LoadAreaByIdAsync(
        int areaId,
        CancellationToken ct)
    {
        return await Context.Groups
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
    }

    /// <summary>
    /// Loads child location groups for the given area IDs.
    /// </summary>
    private async Task<List<Domain.Entities.Group>> LoadLocationsForAreasAsync(
        IEnumerable<int> areaIds,
        CancellationToken ct)
    {
        return await Context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Where(g => g.ParentGroupId.HasValue
                && areaIds.Contains(g.ParentGroupId.Value)
                && g.IsActive
                && !g.IsArchived)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Loads current attendance counts for the given location groups.
    /// </summary>
    private async Task<Dictionary<int, int>> LoadAttendanceCountsAsync(
        List<Domain.Entities.Group> locations,
        DateOnly currentDate,
        CancellationToken ct)
    {
        var locationGroupIds = locations.Select(l => l.Id).ToList();

        if (locationGroupIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await Context.AttendanceOccurrences
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
    }

    // Private helper methods - DTO Building

    /// <summary>
    /// Builds CheckinAreaDto objects for multiple areas.
    /// </summary>
    private List<CheckinAreaDto> BuildAreaDtos(
        List<Domain.Entities.Group> areas,
        List<Domain.Entities.Group> locations,
        Dictionary<int, int> attendanceCounts,
        DateTime currentTime,
        DateOnly currentDate)
    {
        return areas
            .Select(area => BuildAreaDto(area, locations, attendanceCounts, currentTime, currentDate))
            .ToList();
    }

    /// <summary>
    /// Builds a single CheckinAreaDto from loaded data.
    /// </summary>
    private CheckinAreaDto BuildAreaDto(
        Domain.Entities.Group area,
        List<Domain.Entities.Group> allLocations,
        Dictionary<int, int> attendanceCounts,
        DateTime currentTime,
        DateOnly currentDate)
    {
        // Filter locations for this specific area
        var areaLocations = allLocations
            .Where(l => l.ParentGroupId == area.Id)
            .Select(l => MapToCheckinLocationDto(l, attendanceCounts, currentDate))
            .ToList();

        var capacityStatus = CalculateAreaCapacityStatus(areaLocations);
        var scheduleDto = GetAreaScheduleDto(area, currentTime);

        return new CheckinAreaDto
        {
            IdKey = area.IdKey,
            Guid = area.Guid,
            Name = area.Name,
            Description = area.Description,
            GroupType = MapToGroupTypeDto(area.GroupType!),
            Locations = areaLocations,
            Schedule = scheduleDto,
            IsActive = area.IsActive,
            CapacityStatus = capacityStatus,
            MinAgeMonths = area.MinAgeMonths,
            MaxAgeMonths = area.MaxAgeMonths,
            MinGrade = area.MinGrade,
            MaxGrade = area.MaxGrade
        };
    }

    /// <summary>
    /// Gets the active schedule DTO for an area (checks GroupSchedules first, falls back to Schedule).
    /// </summary>
    private ScheduleDto? GetAreaScheduleDto(Domain.Entities.Group area, DateTime currentTime)
    {
        if (area.GroupSchedules?.Any() == true)
        {
            var activeGroupSchedule = area.GroupSchedules
                .Where(gs => gs.Schedule != null && gs.Schedule.IsActive)
                .OrderBy(gs => gs.Order)
                .FirstOrDefault(gs => IsScheduleCheckinActive(gs.Schedule!, currentTime));

            if (activeGroupSchedule?.Schedule != null)
            {
                return MapToScheduleDto(activeGroupSchedule.Schedule, currentTime);
            }
        }

        // Fall back to legacy single Schedule
        if (area.Schedule != null)
        {
            return MapToScheduleDto(area.Schedule, currentTime);
        }

        return null;
    }

    /// <summary>
    /// Maps a GroupType entity to a GroupTypeSummaryDto.
    /// </summary>
    private static GroupTypeSummaryDto MapToGroupTypeDto(Domain.Entities.GroupType groupType)
    {
        return new GroupTypeSummaryDto
        {
            IdKey = groupType.IdKey,
            Guid = groupType.Guid,
            Name = groupType.Name,
            Description = groupType.Description,
            AllowMultipleLocations = groupType.AllowMultipleLocations,
            Roles = groupType.Roles
                .Select(r => new GroupTypeRoleDto
                {
                    IdKey = r.IdKey,
                    Name = r.Name,
                    IsLeader = r.IsLeader
                })
                .ToList()
        };
    }

    // Private helper methods - Mapping

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

        var percentageFull = CalculatePercentageFull(currentCount, locationGroup.GroupCapacity);

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
            PrinterDeviceIdKey = null, // For MVP, printer assignment not yet implemented
            PercentageFull = percentageFull,
            OverflowLocationIdKey = null, // Tech debt: Integrate Location entity for overflow
            OverflowLocationName = null,
            AutoAssignOverflow = false
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

    private static int CalculatePercentageFull(int currentCount, int? capacity)
    {
        if (!capacity.HasValue || capacity.Value == 0)
        {
            return 0;
        }

        return (int)Math.Round((double)currentCount / capacity.Value * 100);
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

        // Validate age and grade ranges for all areas
        foreach (var area in areas)
        {
            ValidateAgeRange(area.MinAgeMonths, area.MaxAgeMonths, area.Name);
            ValidateGradeRange(area.MinGrade, area.MaxGrade, area.Name);
        }

        // Calculate person's age in months and grade
        var personAgeInMonths = _gradeCalculationService.CalculateAgeInMonths(personBirthDate, currentDate);
        var personGrade = _gradeCalculationService.CalculateGrade(personGraduationYear, currentDate);

        // Filter areas based on eligibility
        return areas
            .Where(area => IsAreaEligibleForPerson(area, personAgeInMonths, personGrade))
            .ToList();
    }

    // Private helper methods - Validation

    /// <summary>
    /// Validates that age range is properly configured.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when age range is invalid.</exception>
    private static void ValidateAgeRange(int? minAgeMonths, int? maxAgeMonths, string areaName)
    {
        if (!minAgeMonths.HasValue && !maxAgeMonths.HasValue)
        {
            return; // No age range specified is valid
        }

        // Both values must be non-negative
        if (minAgeMonths < 0)
        {
            throw new ArgumentException(
                $"MinAgeMonths cannot be negative for area '{areaName}'. Value: {minAgeMonths}",
                nameof(minAgeMonths));
        }

        if (maxAgeMonths < 0)
        {
            throw new ArgumentException(
                $"MaxAgeMonths cannot be negative for area '{areaName}'. Value: {maxAgeMonths}",
                nameof(maxAgeMonths));
        }

        // Min must be less than or equal to max when both are specified
        if (minAgeMonths.HasValue && maxAgeMonths.HasValue && minAgeMonths.Value > maxAgeMonths.Value)
        {
            throw new ArgumentException(
                $"MinAgeMonths ({minAgeMonths}) cannot be greater than MaxAgeMonths ({maxAgeMonths}) for area '{areaName}'",
                nameof(minAgeMonths));
        }

        // Reasonable upper bound: 1200 months = 100 years
        const int MaxReasonableAgeMonths = 1200;
        if (minAgeMonths > MaxReasonableAgeMonths)
        {
            throw new ArgumentException(
                $"MinAgeMonths ({minAgeMonths}) exceeds reasonable limit ({MaxReasonableAgeMonths} months) for area '{areaName}'",
                nameof(minAgeMonths));
        }

        if (maxAgeMonths > MaxReasonableAgeMonths)
        {
            throw new ArgumentException(
                $"MaxAgeMonths ({maxAgeMonths}) exceeds reasonable limit ({MaxReasonableAgeMonths} months) for area '{areaName}'",
                nameof(maxAgeMonths));
        }
    }

    /// <summary>
    /// Validates that grade range is properly configured.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when grade range is invalid.</exception>
    private static void ValidateGradeRange(int? minGrade, int? maxGrade, string areaName)
    {
        if (!minGrade.HasValue && !maxGrade.HasValue)
        {
            return; // No grade range specified is valid
        }

        // Reasonable bounds: K-12 system uses -1 (Kindergarten) through 12
        const int MinReasonableGrade = -1;  // Kindergarten
        const int MaxReasonableGrade = 12;  // 12th grade

        if (minGrade < MinReasonableGrade)
        {
            throw new ArgumentException(
                $"MinGrade ({minGrade}) is below reasonable limit ({MinReasonableGrade}) for area '{areaName}'",
                nameof(minGrade));
        }

        if (maxGrade < MinReasonableGrade)
        {
            throw new ArgumentException(
                $"MaxGrade ({maxGrade}) is below reasonable limit ({MinReasonableGrade}) for area '{areaName}'",
                nameof(maxGrade));
        }

        if (minGrade > MaxReasonableGrade)
        {
            throw new ArgumentException(
                $"MinGrade ({minGrade}) exceeds reasonable limit ({MaxReasonableGrade}) for area '{areaName}'",
                nameof(minGrade));
        }

        if (maxGrade > MaxReasonableGrade)
        {
            throw new ArgumentException(
                $"MaxGrade ({maxGrade}) exceeds reasonable limit ({MaxReasonableGrade}) for area '{areaName}'",
                nameof(maxGrade));
        }

        // Min must be less than or equal to max when both are specified
        if (minGrade.HasValue && maxGrade.HasValue && minGrade.Value > maxGrade.Value)
        {
            throw new ArgumentException(
                $"MinGrade ({minGrade}) cannot be greater than MaxGrade ({maxGrade}) for area '{areaName}'",
                nameof(minGrade));
        }
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
