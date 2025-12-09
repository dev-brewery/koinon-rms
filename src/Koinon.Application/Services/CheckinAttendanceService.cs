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
/// Service for check-in attendance operations.
/// Handles recording attendance, generating security codes, and managing check-in/check-out.
/// Performance-critical - all operations optimized for &lt;200ms response time.
/// </summary>
public class CheckinAttendanceService(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger<CheckinAttendanceService> logger,
    ConcurrentOperationHelper concurrencyHelper,
    IFollowUpRetryService followUpRetryService)
    : AuthorizedCheckinService(context, userContext, logger), ICheckinAttendanceService
{
    private readonly ConcurrentOperationHelper _concurrencyHelper = concurrencyHelper;
    private readonly IFollowUpRetryService _followUpRetryService = followUpRetryService;

    public async Task<CheckinResultDto> CheckInAsync(
        CheckinRequestDto request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate and decode IDs
            if (!IdKeyHelper.TryDecode(request.PersonIdKey, out int personId))
            {
                return new CheckinResultDto(
                    Success: false,
                    ErrorMessage: "Invalid person ID");
            }

            if (!IdKeyHelper.TryDecode(request.LocationIdKey, out int locationId))
            {
                return new CheckinResultDto(
                    Success: false,
                    ErrorMessage: "Invalid location ID");
            }

            // Authorization check - throws UnauthorizedAccessException if denied
            AuthorizeCheckinOperation(personId, locationId, nameof(CheckInAsync));

            int? scheduleId = null;
            if (!string.IsNullOrEmpty(request.ScheduleIdKey))
            {
                if (!IdKeyHelper.TryDecode(request.ScheduleIdKey, out int sid))
                {
                    return new CheckinResultDto(
                        Success: false,
                        ErrorMessage: "Invalid schedule ID");
                }
                scheduleId = sid;
            }

            var occurrenceDate = request.OccurrenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // Validate check-in is allowed
            var validation = await ValidateCheckinInternalAsync(personId, locationId, occurrenceDate, ct);
            if (!validation.IsAllowed)
            {
                return new CheckinResultDto(
                    Success: false,
                    ErrorMessage: validation.Reason);
            }

            // Get or create attendance occurrence using atomic helper
            var occurrence = await _concurrencyHelper.GetOrCreateOccurrenceAtomicAsync(
                locationId, scheduleId, occurrenceDate, ct);

            // Check if person already has an active check-in for this occurrence
            // Use subquery to avoid N+1 query (more efficient than Join)
            var personAliasIds = await Context.PersonAliases
                .AsNoTracking()
                .Where(pa => pa.PersonId == personId)
                .Select(pa => pa.Id)
                .ToListAsync(ct);

            var existingAttendance = await Context.Attendances
                .AsNoTracking()
                .Where(a => a.OccurrenceId == occurrence.Id &&
                           a.EndDateTime == null &&
                           a.PersonAliasId.HasValue &&
                           personAliasIds.Contains(a.PersonAliasId.Value))
                .FirstOrDefaultAsync(ct);

            if (existingAttendance != null)
            {
                return new CheckinResultDto(
                    Success: false,
                    ErrorMessage: "Person is already checked in to this location");
            }

            // Get person's primary alias
            var personAlias = await Context.PersonAliases
                .AsNoTracking()
                .FirstOrDefaultAsync(pa => pa.PersonId == personId && pa.AliasPersonId == null, ct);

            if (personAlias == null)
            {
                return new CheckinResultDto(
                    Success: false,
                    ErrorMessage: "Person alias not found");
            }

            // Generate security code if requested using atomic helper
            AttendanceCode? attendanceCode = null;
            if (request.GenerateSecurityCode)
            {
                attendanceCode = await _concurrencyHelper.GenerateSecurityCodeAtomicAsync(occurrenceDate, ct: ct);
            }

            // Determine if this is first time attendance
            var isFirstTime = await IsFirstTimeAttendanceAsync(personId, locationId, ct);

            // Create attendance record
            var attendance = new Attendance
            {
                OccurrenceId = occurrence.Id,
                PersonAliasId = personAlias.Id,
                AttendanceCodeId = attendanceCode?.Id,
                StartDateTime = DateTime.UtcNow,
                Note = request.Note,
                IsFirstTime = isFirstTime,
                DidAttend = true,
                PresentDateTime = DateTime.UtcNow
            };

            Context.Attendances.Add(attendance);
            await Context.SaveChangesAsync(ct);

            // Create follow-up for first-time visitors
            if (isFirstTime)
            {
                try
                {
                    // Queue follow-up creation with automatic retry on failure
                    _followUpRetryService.QueueFollowUpCreation(personId, attendance.Id);
                    Logger.LogInformation(
                        "Queued follow-up creation for first-time visitor PersonId={PersonId} AttendanceId={AttendanceId}",
                        personId, attendance.Id);
                }
                catch (Exception ex)
                {
                    // CRITICAL: Don't fail check-in (performance-critical path), but log queueing failure
                    // This should be extremely rare (only if Hangfire is down or misconfigured)
                    Logger.LogError(ex,
                        "FOLLOW_UP_QUEUE_FAILED: Unable to queue follow-up creation. " +
                        "PersonId={PersonId} AttendanceId={AttendanceId} AttendanceIdKey={AttendanceIdKey}. " +
                        "Check-in succeeded but follow-up may not be created. Check Hangfire status.",
                        personId, attendance.Id, attendance.IdKey);
                }
            }

            // Get person and location details for response
            var person = await Context.People
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == personId, ct);

            var location = await Context.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == locationId, ct);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 200)
            {
                Logger.LogWarning(
                    "Check-in exceeded 200ms target: {Elapsed}ms for person {PersonId} at location {LocationId}",
                    stopwatch.ElapsedMilliseconds, personId, locationId);
            }
            else
            {
                Logger.LogInformation(
                    "Check-in completed in {Elapsed}ms for person {PersonId} at location {LocationId}",
                    stopwatch.ElapsedMilliseconds, personId, locationId);
            }

            return new CheckinResultDto(
                Success: true,
                AttendanceIdKey: attendance.IdKey,
                SecurityCode: attendanceCode?.Code,
                CheckInTime: attendance.StartDateTime,
                Person: person != null ? new CheckinPersonSummaryDto(
                    IdKey: person.IdKey,
                    FullName: person.FullName,
                    FirstName: person.FirstName,
                    LastName: person.LastName,
                    NickName: person.NickName,
                    Age: CalculateAge(person.BirthDate),
                    PhotoUrl: null) : null,
                Location: location != null ? new CheckinLocationSummaryDto(
                    IdKey: location.IdKey,
                    Name: location.Name,
                    FullPath: location.Name) : null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking in person {PersonIdKey} to location {LocationIdKey}",
                request.PersonIdKey, request.LocationIdKey);

            return new CheckinResultDto(
                Success: false,
                ErrorMessage: "An unexpected error occurred during check-in");
        }
    }

    public async Task<BatchCheckinResultDto> BatchCheckInAsync(
        BatchCheckinRequestDto request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Process check-ins in parallel for performance
        var tasks = request.CheckIns.Select(checkinRequest =>
        {
            // Apply device from batch request if not specified in individual request
            var individualRequest = checkinRequest with
            {
                DeviceIdKey = checkinRequest.DeviceIdKey ?? request.DeviceIdKey
            };
            return CheckInAsync(individualRequest, ct);
        });

        var results = (await Task.WhenAll(tasks)).ToList();

        stopwatch.Stop();

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        if (stopwatch.ElapsedMilliseconds > 500)
        {
            Logger.LogWarning(
                "Batch check-in exceeded 500ms target: {Elapsed}ms for {Count} people ({Success} succeeded, {Failed} failed)",
                stopwatch.ElapsedMilliseconds, results.Count, successCount, failureCount);
        }
        else
        {
            Logger.LogInformation(
                "Batch check-in completed in {Elapsed}ms for {Count} people ({Success} succeeded, {Failed} failed)",
                stopwatch.ElapsedMilliseconds, results.Count, successCount, failureCount);
        }

        return new BatchCheckinResultDto(
            Results: results,
            SuccessCount: successCount,
            FailureCount: failureCount,
            AllSucceeded: failureCount == 0);
    }

    public async Task<bool> CheckOutAsync(string attendanceIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(attendanceIdKey, out int attendanceId))
        {
            Logger.LogWarning("Invalid attendance ID key for check-out: {IdKey}", attendanceIdKey);
            return false;
        }

        var attendance = await Context.Attendances
            .Include(a => a.PersonAlias)
            .Include(a => a.Occurrence)
            .FirstOrDefaultAsync(a => a.Id == attendanceId && a.EndDateTime == null, ct);

        if (attendance == null)
        {
            Logger.LogWarning("Attendance {AttendanceId} not found or already checked out", attendanceId);
            return false;
        }

        // Authorization check - verify user can access the person being checked out
        if (attendance.PersonAlias?.PersonId == null)
        {
            Logger.LogWarning("Attendance {AttendanceId} has no associated person", attendanceId);
            return false;
        }

        // SECURITY: Location authorization is MANDATORY - cannot be skipped
        if (attendance.Occurrence?.GroupId == null)
        {
            Logger.LogWarning("Attendance {AttendanceId} missing location context", attendanceId);
            return false;
        }

        try
        {
            AuthorizePersonAccess(attendance.PersonAlias.PersonId.Value, nameof(CheckOutAsync));
            AuthorizeLocationAccess(attendance.Occurrence.GroupId.Value, nameof(CheckOutAsync));
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        attendance.EndDateTime = DateTime.UtcNow;
        await Context.SaveChangesAsync(ct);

        Logger.LogInformation("Checked out attendance {AttendanceId}", attendanceId);
        return true;
    }

    public async Task<IReadOnlyList<AttendanceSummaryDto>> GetCurrentAttendanceAsync(
        string locationIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            return Array.Empty<AttendanceSummaryDto>();
        }

        // Authorization check - throws if denied
        try
        {
            AuthorizeLocationAccess(locationId, nameof(GetCurrentAttendanceAsync));
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<AttendanceSummaryDto>();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get attendance for today at this location
        // Query Attendances directly to allow Include before materialization
        var attendances = await Context.Attendances
            .AsNoTracking()
            .Include(a => a.AttendanceCode)
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Group)
            .Where(a => a.Occurrence != null
                && a.Occurrence.GroupId == locationId
                && a.Occurrence.OccurrenceDate == today
                && a.EndDateTime == null) // Only currently checked in
            .ToListAsync(ct);

        // Get all person alias IDs
        var personAliasIds = attendances
            .Where(a => a.PersonAliasId.HasValue)
            .Select(a => a.PersonAliasId!.Value)
            .Distinct()
            .ToList();

        // Batch load people
        var people = await Context.PersonAliases
            .AsNoTracking()
            .Where(pa => personAliasIds.Contains(pa.Id))
            .Join(Context.People,
                pa => pa.PersonId,
                p => p.Id,
                (pa, p) => new { PersonAliasId = pa.Id, Person = p })
            .ToDictionaryAsync(x => x.PersonAliasId, x => x.Person, ct);

        var results = new List<AttendanceSummaryDto>();

        foreach (var attendance in attendances)
        {
            if (!attendance.PersonAliasId.HasValue || !people.TryGetValue(attendance.PersonAliasId.Value, out var person))
            {
                Logger.LogWarning(
                    "Attendance {AttendanceId} has missing PersonAlias data - skipping",
                    attendance.Id);
                continue;
            }

            // Validate required navigation properties
            if (attendance.Occurrence?.Group == null)
            {
                Logger.LogWarning(
                    "Attendance {AttendanceId} has missing Occurrence or Group data - skipping",
                    attendance.Id);
                continue;
            }

            results.Add(new AttendanceSummaryDto(
                IdKey: attendance.IdKey,
                Person: new CheckinPersonSummaryDto(
                    IdKey: person.IdKey,
                    FullName: person.FullName,
                    FirstName: person.FirstName,
                    LastName: person.LastName,
                    NickName: person.NickName,
                    Age: CalculateAge(person.BirthDate),
                    PhotoUrl: null),
                Location: new CheckinLocationSummaryDto(
                    IdKey: attendance.Occurrence.Group.IdKey,
                    Name: attendance.Occurrence.Group.Name,
                    FullPath: attendance.Occurrence.Group.Name),
                StartDateTime: attendance.StartDateTime,
                EndDateTime: attendance.EndDateTime,
                SecurityCode: attendance.AttendanceCode?.Code,
                IsFirstTime: attendance.IsFirstTime,
                Note: attendance.Note));
        }

        return results.OrderBy(a => a.Person.LastName).ThenBy(a => a.Person.FirstName).ToList();
    }

    public async Task<IReadOnlyList<AttendanceSummaryDto>> GetPersonAttendanceHistoryAsync(
        string personIdKey,
        int days = 30,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Array.Empty<AttendanceSummaryDto>();
        }

        // Authorization check - throws if denied
        try
        {
            AuthorizePersonAccess(personId, nameof(GetPersonAttendanceHistoryAsync));
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<AttendanceSummaryDto>();
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        // Get person's aliases
        var personAliasIds = await Context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId == personId)
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        if (personAliasIds.Count == 0)
        {
            return Array.Empty<AttendanceSummaryDto>();
        }

        // Get attendance history
        var attendances = await Context.Attendances
            .AsNoTracking()
            .Where(a => a.PersonAliasId.HasValue &&
                       personAliasIds.Contains(a.PersonAliasId.Value) &&
                       a.StartDateTime >= cutoffDate)
            .Include(a => a.AttendanceCode)
            .Include(a => a.Occurrence)
                .ThenInclude(o => o!.Group)
            .OrderByDescending(a => a.StartDateTime)
            .ToListAsync(ct);

        var person = await Context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (person == null)
        {
            return Array.Empty<AttendanceSummaryDto>();
        }

        var results = new List<AttendanceSummaryDto>();

        foreach (var attendance in attendances)
        {
            // Validate required navigation properties
            if (attendance.Occurrence?.Group == null)
            {
                Logger.LogWarning(
                    "Attendance {AttendanceId} has missing Occurrence or Group data - skipping",
                    attendance.Id);
                continue;
            }

            results.Add(new AttendanceSummaryDto(
                IdKey: attendance.IdKey,
                Person: new CheckinPersonSummaryDto(
                    IdKey: person.IdKey,
                    FullName: person.FullName,
                    FirstName: person.FirstName,
                    LastName: person.LastName,
                    NickName: person.NickName,
                    Age: CalculateAge(person.BirthDate),
                    PhotoUrl: null),
                Location: new CheckinLocationSummaryDto(
                    IdKey: attendance.Occurrence.Group.IdKey,
                    Name: attendance.Occurrence.Group.Name,
                    FullPath: attendance.Occurrence.Group.Name),
                StartDateTime: attendance.StartDateTime,
                EndDateTime: attendance.EndDateTime,
                SecurityCode: attendance.AttendanceCode?.Code,
                IsFirstTime: attendance.IsFirstTime,
                Note: attendance.Note));
        }

        return results;
    }

    public async Task<CheckinValidationResult> ValidateCheckinAsync(
        string personIdKey,
        string locationIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return new CheckinValidationResult(
                IsAllowed: false,
                Reason: "Invalid person ID");
        }

        if (!IdKeyHelper.TryDecode(locationIdKey, out int locationId))
        {
            return new CheckinValidationResult(
                IsAllowed: false,
                Reason: "Invalid location ID");
        }

        // Authorization check - throws if denied
        try
        {
            AuthorizeCheckinOperation(personId, locationId, nameof(ValidateCheckinAsync));
        }
        catch (UnauthorizedAccessException)
        {
            return new CheckinValidationResult(
                IsAllowed: false,
                Reason: "Not authorized for this operation");
        }

        return await ValidateCheckinInternalAsync(personId, locationId, null, ct);
    }

    // Private helper methods

    private async Task<CheckinValidationResult> ValidateCheckinInternalAsync(
        int personId,
        int locationId,
        DateOnly? occurrenceDate,
        CancellationToken ct)
    {
        occurrenceDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Check if person exists and is active
        var person = await Context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (person == null)
        {
            return new CheckinValidationResult(
                IsAllowed: false,
                Reason: "Person not found");
        }

        if (person.IsDeceased)
        {
            return new CheckinValidationResult(
                IsAllowed: false,
                Reason: "Person is marked as deceased");
        }

        // Check if location exists and is active
        var location = await Context.Groups
            .AsNoTracking()
            .Include(g => g.Schedule)
            .FirstOrDefaultAsync(g => g.Id == locationId, ct);

        if (location == null)
        {
            return new CheckinValidationResult(
                IsAllowed: false,
                Reason: "Location not found");
        }

        if (!location.IsActive || location.IsArchived)
        {
            return new CheckinValidationResult(
                IsAllowed: false,
                Reason: "Location is not active");
        }

        // Check if already checked in today at this location
        // Get person alias IDs for this person
        var personAliasIdsForCheck = await Context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId == personId)
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        var existingOccurrence = await Context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.GroupId == locationId && o.OccurrenceDate == occurrenceDate.Value)
            .SelectMany(o => o.Attendances)
            .Where(a => a.EndDateTime == null && a.PersonAliasId.HasValue)
            .AnyAsync(a => personAliasIdsForCheck.Contains(a.PersonAliasId!.Value), ct);

        if (existingOccurrence)
        {
            return new CheckinValidationResult(
                false,
                "Person is already checked in to this location",
                IsAlreadyCheckedIn: true);
        }

        // Check capacity if configured
        if (location.GroupCapacity.HasValue)
        {
            var currentCount = await Context.AttendanceOccurrences
                .AsNoTracking()
                .Where(o => o.GroupId == locationId && o.OccurrenceDate == occurrenceDate.Value)
                .SelectMany(o => o.Attendances)
                .CountAsync(a => a.EndDateTime == null, ct);

            if (currentCount >= location.GroupCapacity.Value)
            {
                return new CheckinValidationResult(
                    false,
                    "Location is at capacity",
                    IsAtCapacity: true);
            }
        }

        // Check schedule window if configured
        if (location.Schedule != null)
        {
            var currentTime = DateTime.UtcNow;
            var isCheckinOpen = IsScheduleCheckinActive(location.Schedule, currentTime);

            if (!isCheckinOpen)
            {
                return new CheckinValidationResult(
                    false,
                    "Check-in is outside the schedule window",
                    IsOutsideSchedule: true);
            }
        }

        return new CheckinValidationResult(true);
    }

    private async Task<bool> IsFirstTimeAttendanceAsync(int personId, int locationId, CancellationToken ct)
    {
        // Get person alias IDs for this person
        var personAliasIdsForFirstTime = await Context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId == personId)
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        // Check if person has any previous attendance at this location
        var hasPreviousAttendance = await Context.AttendanceOccurrences
            .AsNoTracking()
            .Where(o => o.GroupId == locationId)
            .SelectMany(o => o.Attendances)
            .Where(a => a.PersonAliasId.HasValue)
            .AnyAsync(a => personAliasIdsForFirstTime.Contains(a.PersonAliasId!.Value), ct);

        return !hasPreviousAttendance;
    }

    private static bool IsScheduleCheckinActive(Schedule schedule, DateTime currentTime)
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
}
