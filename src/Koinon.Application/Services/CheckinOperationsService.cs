using Koinon.Application.Common;
using Koinon.Application.DTOs.CheckinOperations;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Thin orchestration service that backs the live check-in operations dashboard (#482).
/// Reads from the existing check-in / attendance data and composes a flat payload suitable
/// for 5-second client polling. Does NOT duplicate check-in logic — it reuses the same
/// Location + AttendanceOccurrence tables populated by <see cref="CheckinAttendanceService"/>.
/// </summary>
public class CheckinOperationsService(
    IApplicationDbContext context,
    ILogger<CheckinOperationsService> logger) : ICheckinOperationsService
{
    /// <inheritdoc />
    public async Task<Result<CheckinOperationsDashboardDto>> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Active locations that look like check-in rooms: active + at least one capacity threshold.
        //    This mirrors what CapacityService treats as a "room" and avoids pulling in non-room
        //    locations like whole campuses.
        var locations = await context.Locations
            .AsNoTracking()
            .Where(l => l.IsActive
                && (l.SoftRoomThreshold != null || l.FirmRoomThreshold != null))
            .OrderBy(l => l.Name)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.SoftRoomThreshold,
                l.FirmRoomThreshold,
                l.IsOpen,
            })
            .ToListAsync(ct);

        if (locations.Count == 0)
        {
            var empty = new CheckinOperationsDashboardDto(
                Rooms: Array.Empty<CheckinOperationsRoomDto>(),
                Attendees: Array.Empty<CheckinOperationsAttendeeDto>(),
                Summary: new CheckinOperationsSummaryDto(0, 0, 0),
                GeneratedAt: DateTime.UtcNow);
            return Result<CheckinOperationsDashboardDto>.Success(empty);
        }

        var locationIds = locations.Select(l => l.Id).ToList();

        // 2. Today's attendance rows for these "locations" (stored under occurrence.GroupId per
        //    the existing CheckinAttendanceService convention — see CapacityService for the same
        //    pattern). Both currently-checked-in and already-checked-out rows are returned so
        //    the summary can distinguish them.
        var attendanceRows = await context.Attendances
            .AsNoTracking()
            .Where(a => a.Occurrence != null
                && a.Occurrence.OccurrenceDate == today
                && a.Occurrence.GroupId != null
                && locationIds.Contains(a.Occurrence.GroupId.Value))
            .Select(a => new
            {
                AttendanceId = a.Id,
                LocationId = a.Occurrence!.GroupId!.Value,
                a.StartDateTime,
                a.EndDateTime,
                a.PersonAliasId,
            })
            .ToListAsync(ct);

        // 3. Map person-alias to person for attendee display.
        var personAliasIds = attendanceRows
            .Where(r => r.PersonAliasId.HasValue)
            .Select(r => r.PersonAliasId!.Value)
            .Distinct()
            .ToList();

        var peopleByAlias = personAliasIds.Count == 0
            ? new Dictionary<int, (string FullName, string PersonIdKey)>()
            : (await context.PersonAliases
                .AsNoTracking()
                .Where(pa => personAliasIds.Contains(pa.Id))
                .Join(context.People.AsNoTracking(),
                    pa => pa.PersonId,
                    p => p.Id,
                    (pa, p) => new { AliasId = pa.Id, p.FullName, PersonId = p.Id })
                .ToListAsync(ct))
                .ToDictionary(
                    x => x.AliasId,
                    x => (FullName: x.FullName, PersonIdKey: IdKeyHelper.Encode(x.PersonId)));

        var locationNameById = locations.ToDictionary(
            l => l.Id,
            l => l.Name);

        // 4. Build the flat attendee list.
        var attendees = attendanceRows
            .Where(r => r.PersonAliasId.HasValue && peopleByAlias.ContainsKey(r.PersonAliasId.Value))
            .Select(r =>
            {
                var person = peopleByAlias[r.PersonAliasId!.Value];
                return new CheckinOperationsAttendeeDto(
                    AttendanceIdKey: IdKeyHelper.Encode(r.AttendanceId),
                    PersonIdKey: person.PersonIdKey,
                    FullName: person.FullName,
                    LocationIdKey: IdKeyHelper.Encode(r.LocationId),
                    LocationName: locationNameById.GetValueOrDefault(r.LocationId, string.Empty),
                    CheckInTime: r.StartDateTime,
                    CheckOutTime: r.EndDateTime,
                    IsPresent: r.EndDateTime == null);
            })
            .OrderByDescending(a => a.CheckInTime)
            .ToList();

        // 5. Per-room counts (currently present only) + capacity pill color.
        var presentCountsByLocation = attendanceRows
            .Where(r => r.EndDateTime == null)
            .GroupBy(r => r.LocationId)
            .ToDictionary(g => g.Key, g => g.Count());

        var rooms = locations.Select(l =>
        {
            var count = presentCountsByLocation.GetValueOrDefault(l.Id, 0);
            var capacity = l.FirmRoomThreshold ?? l.SoftRoomThreshold;
            var percent = capacity is > 0
                ? (int)Math.Round(count * 100.0 / capacity.Value)
                : 0;
            var color = !l.IsOpen
                ? "grey"
                : percent > 80
                    ? "red"
                    : percent >= 50
                        ? "yellow"
                        : "green";
            return new CheckinOperationsRoomDto(
                LocationIdKey: IdKeyHelper.Encode(l.Id),
                LocationName: l.Name,
                CheckedInCount: count,
                Capacity: capacity,
                PercentFull: percent,
                CapacityPillColor: color,
                IsOpen: l.IsOpen);
        }).ToList();

        // 6. Summary. "Total checked in" = distinct people seen today; "present" = still in;
        //    "checked out" = total - present.
        var totalCheckedIn = attendanceRows
            .Where(r => r.PersonAliasId.HasValue)
            .Select(r => r.PersonAliasId!.Value)
            .Distinct()
            .Count();
        var currentlyPresent = attendanceRows.Count(r => r.EndDateTime == null);
        var checkedOut = Math.Max(0, totalCheckedIn - currentlyPresent);

        var summary = new CheckinOperationsSummaryDto(
            TotalCheckedIn: totalCheckedIn,
            CurrentlyPresent: currentlyPresent,
            CheckedOut: checkedOut);

        logger.LogInformation(
            "Check-in operations dashboard: {Rooms} rooms, {Present} present, {Total} total today",
            rooms.Count, currentlyPresent, totalCheckedIn);

        return Result<CheckinOperationsDashboardDto>.Success(new CheckinOperationsDashboardDto(
            Rooms: rooms,
            Attendees: attendees,
            Summary: summary,
            GeneratedAt: DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task<Result<ToggleRoomResponseDto>> ToggleRoomAsync(
        string locationIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            return Result<ToggleRoomResponseDto>.Failure(
                Error.Validation($"Invalid location IdKey '{locationIdKey}'"));
        }

        // The DbContext defaults to NoTracking for read perf — force tracking here
        // so the IsOpen mutation is picked up by SaveChangesAsync.
        var location = await context.Locations
            .AsTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId, ct);

        if (location is null)
        {
            return Result<ToggleRoomResponseDto>.Failure(Error.NotFound("Location", locationIdKey));
        }

        location.IsOpen = !location.IsOpen;
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Room toggled: LocationId={LocationId}, IsOpen={IsOpen}",
            locationId, location.IsOpen);

        return Result<ToggleRoomResponseDto>.Success(new ToggleRoomResponseDto(
            LocationIdKey: locationIdKey,
            IsOpen: location.IsOpen));
    }
}
