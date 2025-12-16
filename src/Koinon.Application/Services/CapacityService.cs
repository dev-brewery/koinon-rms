using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for room capacity management operations.
/// Handles capacity tracking, overflow management, and staff ratio enforcement.
/// </summary>
public class CapacityService(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger<CapacityService> logger)
    : AuthorizedCheckinService(context, userContext, logger), ICapacityService
{
    public async Task<RoomCapacityDto?> GetLocationCapacityAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetLocationCapacityAsync));

        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            return null;
        }

        var location = await Context.Locations
            .AsNoTracking()
            .Include(l => l.OverflowLocation)
            .FirstOrDefaultAsync(l => l.Id == locationId, ct);

        if (location is null)
        {
            return null;
        }

        occurrenceDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Get current attendance count
        var currentCount = await GetAttendanceCountAsync(locationId, occurrenceDate.Value, ct);

        // Get staff count (members with staff role in this location)
        var staffCount = await GetStaffCountAsync(locationId, occurrenceDate.Value, ct);

        // Calculate capacity status
        var softCapacity = location.SoftRoomThreshold;
        var hardCapacity = location.FirmRoomThreshold;

        // Get overflow location info
        string? overflowIdKey = location.OverflowLocation?.IdKey;
        string? overflowName = location.OverflowLocation?.Name;
        bool autoAssignOverflow = location.AutoAssignOverflow;

        var capacityStatus = CalculateCapacityStatus(currentCount, softCapacity, hardCapacity);
        var percentageFull = CalculatePercentageFull(currentCount, softCapacity);

        // Calculate staff ratio
        int? requiredStaffCount = null;
        bool meetsStaffRatio = true;
        var staffToChildRatio = location.StaffToChildRatio;

        if (staffToChildRatio.HasValue && currentCount > 0)
        {
            requiredStaffCount = (int)Math.Ceiling((double)currentCount / staffToChildRatio.Value);
            meetsStaffRatio = staffCount >= requiredStaffCount;
        }

        Logger.LogInformation(
            "Retrieved capacity for location {LocationId}: Current={Current}, Soft={Soft}, Status={Status}",
            locationId, currentCount, softCapacity, capacityStatus);

        return new RoomCapacityDto
        {
            IdKey = location.IdKey,
            Name = location.Name,
            SoftCapacity = softCapacity,
            HardCapacity = hardCapacity,
            CurrentCount = currentCount,
            CapacityStatus = capacityStatus,
            PercentageFull = percentageFull,
            StaffToChildRatio = staffToChildRatio,
            CurrentStaffCount = staffCount,
            RequiredStaffCount = requiredStaffCount,
            MeetsStaffRatio = meetsStaffRatio,
            OverflowLocationIdKey = overflowIdKey,
            OverflowLocationName = overflowName,
            AutoAssignOverflow = autoAssignOverflow,
            IsActive = location.IsActive
        };
    }

    public async Task<bool> UpdateCapacitySettingsAsync(
        string locationIdKey,
        UpdateCapacitySettingsDto settings,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(UpdateCapacitySettingsAsync));

        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            return false;
        }

        var location = await Context.Locations
            .FirstOrDefaultAsync(l => l.Id == locationId, ct);

        if (location is null)
        {
            return false;
        }

        // Update capacity settings
        location.SoftRoomThreshold = settings.SoftCapacity;
        location.FirmRoomThreshold = settings.HardCapacity;
        location.StaffToChildRatio = settings.StaffToChildRatio;

        if (settings.OverflowLocationIdKey is not null)
        {
            if (IdKeyHelper.TryDecode(settings.OverflowLocationIdKey, out int overflowId))
            {
                location.OverflowLocationId = overflowId;
            }
        }
        else
        {
            location.OverflowLocationId = null;
        }

        location.AutoAssignOverflow = settings.AutoAssignOverflow;

        await Context.SaveChangesAsync(ct);

        Logger.LogInformation(
            "Updated capacity settings for location {LocationId}: Soft={Soft}, Hard={Hard}, StaffRatio={Ratio}",
            locationId, settings.SoftCapacity, settings.HardCapacity, settings.StaffToChildRatio);

        return true;
    }

    public async Task<bool> CanAcceptCheckinAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(CanAcceptCheckinAsync));

        var capacity = await GetLocationCapacityAsync(locationIdKey, occurrenceDate, ct);

        if (capacity is null)
        {
            return false;
        }

        // Can accept if not at hard capacity
        if (capacity.HardCapacity.HasValue && capacity.CurrentCount >= capacity.HardCapacity.Value)
        {
            Logger.LogWarning(
                "Location {LocationIdKey} is at hard capacity: {Current}/{Hard}",
                locationIdKey, capacity.CurrentCount, capacity.HardCapacity);
            return false;
        }

        // If no hard capacity, always allow (soft capacity is just a warning)
        return true;
    }

    public async Task<RoomCapacityDto?> GetOverflowLocationAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetOverflowLocationAsync));

        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            return null;
        }

        var location = await Context.Locations
            .AsNoTracking()
            .Include(l => l.OverflowLocation)
            .FirstOrDefaultAsync(l => l.Id == locationId, ct);

        if (location?.OverflowLocationId is null)
        {
            return null;
        }

        // Get capacity info for overflow location
        return await GetLocationCapacityAsync(location.OverflowLocation!.IdKey, occurrenceDate, ct);
    }

    public async Task<bool> ValidateStaffRatioAsync(
        string locationIdKey,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(ValidateStaffRatioAsync));

        var capacity = await GetLocationCapacityAsync(locationIdKey, occurrenceDate, ct);

        if (capacity is null)
        {
            return false;
        }

        // If no ratio configured or met, return true
        return capacity.MeetsStaffRatio;
    }

    public async Task<IReadOnlyList<RoomCapacityDto>> GetMultipleLocationCapacitiesAsync(
        IEnumerable<string> locationIdKeys,
        DateOnly? occurrenceDate = null,
        CancellationToken ct = default)
    {
        AuthorizeAuthentication(nameof(GetMultipleLocationCapacitiesAsync));

        // Decode all IdKeys at once
        var locationIds = locationIdKeys
            .Select(idKey => IdKeyHelper.TryDecode(idKey, out int id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (locationIds.Count == 0)
        {
            return Array.Empty<RoomCapacityDto>();
        }

        occurrenceDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Single query to load all locations with their overflow locations
        var locations = await Context.Locations
            .AsNoTracking()
            .Include(l => l.OverflowLocation)
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(ct);

        // Single query to load all attendance counts for all locations
        var attendanceCounts = await Context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.OccurrenceDate == occurrenceDate.Value && locationIds.Contains(o.GroupId ?? 0))
            .Select(o => new { o.GroupId, Count = o.Attendances.Count(a => a.EndDateTime == null) })
            .ToListAsync(ct);

        var attendanceDict = attendanceCounts.ToDictionary(a => a.GroupId ?? 0, a => a.Count);

        // Single query to load all staff counts for all locations
        var staffCounts = await Context.Attendances
            .AsNoTracking()
            .Where(a => a.Occurrence!.OccurrenceDate == occurrenceDate.Value
                && locationIds.Contains(a.Occurrence.GroupId ?? 0)
                && a.EndDateTime == null
                && Context.GroupMembers.Any(m => m.Person!.Id == a.PersonAlias!.PersonId
                    && locationIds.Contains(m.GroupId)
                    && m.GroupRole != null && m.GroupRole.IsLeader))
            .GroupBy(a => a.Occurrence!.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var staffDict = staffCounts.ToDictionary(s => s.GroupId ?? 0, s => s.Count);

        // Map results to DTOs
        var results = locations.Select(location =>
        {
            var currentCount = attendanceDict.GetValueOrDefault(location.Id, 0);
            var staffCount = staffDict.GetValueOrDefault(location.Id, 0);

            var softCapacity = location.SoftRoomThreshold;
            var hardCapacity = location.FirmRoomThreshold;

            var capacityStatus = CalculateCapacityStatus(currentCount, softCapacity, hardCapacity);
            var percentageFull = CalculatePercentageFull(currentCount, softCapacity);

            // Calculate staff ratio
            int? requiredStaffCount = null;
            bool meetsStaffRatio = true;
            var staffToChildRatio = location.StaffToChildRatio;

            if (staffToChildRatio.HasValue && currentCount > 0)
            {
                requiredStaffCount = (int)Math.Ceiling((double)currentCount / staffToChildRatio.Value);
                meetsStaffRatio = staffCount >= requiredStaffCount;
            }

            return new RoomCapacityDto
            {
                IdKey = location.IdKey,
                Name = location.Name,
                SoftCapacity = softCapacity,
                HardCapacity = hardCapacity,
                CurrentCount = currentCount,
                CapacityStatus = capacityStatus,
                PercentageFull = percentageFull,
                StaffToChildRatio = staffToChildRatio,
                CurrentStaffCount = staffCount,
                RequiredStaffCount = requiredStaffCount,
                MeetsStaffRatio = meetsStaffRatio,
                OverflowLocationIdKey = location.OverflowLocation?.IdKey,
                OverflowLocationName = location.OverflowLocation?.Name,
                AutoAssignOverflow = location.AutoAssignOverflow,
                IsActive = location.IsActive
            };
        }).ToList();

        Logger.LogInformation(
            "Retrieved capacity for {Count} locations",
            results.Count);

        return results;
    }

    // Private helper methods

    private async Task<int> GetAttendanceCountAsync(int locationId, DateOnly occurrenceDate, CancellationToken ct)
    {
        return await Context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.OccurrenceDate == occurrenceDate && o.GroupId == locationId)
            .Select(o => o.Attendances.Count(a => a.EndDateTime == null))
            .FirstOrDefaultAsync(ct);
    }

    private async Task<int> GetStaffCountAsync(int locationId, DateOnly occurrenceDate, CancellationToken ct)
    {
        // For MVP, staff are GroupMembers with a staff/leader role
        // Count active members with staff roles checked in today
        return await Context.Attendances
            .AsNoTracking()
            .Where(a => a.Occurrence!.OccurrenceDate == occurrenceDate
                && a.Occurrence.GroupId == locationId
                && a.EndDateTime == null
                && Context.GroupMembers.Any(m => m.Person!.Id == a.PersonAlias!.PersonId
                    && m.GroupId == locationId
                    && m.GroupRole != null && m.GroupRole.IsLeader))
            .CountAsync(ct);
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

    private static int CalculatePercentageFull(int currentCount, int? capacity)
    {
        if (!capacity.HasValue || capacity.Value == 0)
        {
            return 0;
        }

        return (int)Math.Round((double)currentCount / capacity.Value * 100);
    }
}
