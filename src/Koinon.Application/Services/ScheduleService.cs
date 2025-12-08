using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for schedule management operations.
/// </summary>
public class ScheduleService(
    IApplicationDbContext context,
    IMapper mapper,
    IValidator<CreateScheduleRequest> createValidator,
    IValidator<UpdateScheduleRequest> updateValidator,
    ILogger<ScheduleService> logger) : IScheduleService
{
    public async Task<ScheduleDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return schedule == null ? null : MapToScheduleDto(schedule);
    }

    public async Task<ScheduleDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<PagedResult<ScheduleSummaryDto>> SearchAsync(
        ScheduleSearchParameters parameters,
        CancellationToken ct = default)
    {
        var query = context.Schedules.AsNoTracking();

        // Apply search query
        if (!string.IsNullOrWhiteSpace(parameters.Query))
        {
            var searchTerm = $"%{parameters.Query}%";
            query = query.Where(s =>
                EF.Functions.Like(s.Name, searchTerm) ||
                (s.Description != null && EF.Functions.Like(s.Description, searchTerm))
            );
        }

        // Filter by day of week
        if (parameters.DayOfWeek.HasValue)
        {
            query = query.Where(s => s.WeeklyDayOfWeek == parameters.DayOfWeek.Value);
        }

        // Filter by active status
        if (!parameters.IncludeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Apply pagination
        var schedules = await query
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Name)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ProjectTo<ScheduleSummaryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<ScheduleSummaryDto>(
            schedules,
            totalCount,
            parameters.Page,
            parameters.PageSize);
    }

    public async Task<Result<ScheduleDto>> CreateAsync(
        CreateScheduleRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<ScheduleDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        var schedule = mapper.Map<Schedule>(request);
        schedule.Guid = Guid.NewGuid();
        schedule.CreatedDateTime = DateTime.UtcNow;

        context.Schedules.Add(schedule);
        await context.SaveChangesAsync(ct);

        var dto = MapToScheduleDto(schedule);

        logger.LogInformation(
            "Schedule created: Id={Id}, Name={Name}",
            schedule.Id, schedule.Name);

        return Result<ScheduleDto>.Success(dto);
    }

    public async Task<Result<ScheduleDto>> UpdateAsync(
        string idKey,
        UpdateScheduleRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<ScheduleDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        // Decode IdKey
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<ScheduleDto>.Failure(Error.NotFound("Schedule", idKey));
        }

        // Find schedule
        var schedule = await context.Schedules.FindAsync([id], ct);
        if (schedule == null)
        {
            return Result<ScheduleDto>.Failure(Error.NotFound("Schedule", idKey));
        }

        // Update properties
        if (request.Name != null)
        {
            schedule.Name = request.Name;
        }

        if (request.Description != null)
        {
            schedule.Description = request.Description;
        }

        if (request.WeeklyDayOfWeek.HasValue)
        {
            schedule.WeeklyDayOfWeek = request.WeeklyDayOfWeek;
        }

        if (request.WeeklyTimeOfDay.HasValue)
        {
            schedule.WeeklyTimeOfDay = request.WeeklyTimeOfDay;
        }

        if (request.CheckInStartOffsetMinutes.HasValue)
        {
            schedule.CheckInStartOffsetMinutes = request.CheckInStartOffsetMinutes;
        }

        if (request.CheckInEndOffsetMinutes.HasValue)
        {
            schedule.CheckInEndOffsetMinutes = request.CheckInEndOffsetMinutes;
        }

        if (request.EffectiveStartDate.HasValue)
        {
            schedule.EffectiveStartDate = request.EffectiveStartDate;
        }

        if (request.EffectiveEndDate.HasValue)
        {
            schedule.EffectiveEndDate = request.EffectiveEndDate;
        }

        if (request.IsActive.HasValue)
        {
            schedule.IsActive = request.IsActive.Value;
        }

        if (request.IsPublic.HasValue)
        {
            schedule.IsPublic = request.IsPublic.Value;
        }

        if (request.Order.HasValue)
        {
            schedule.Order = request.Order.Value;
        }

        if (request.ICalendarContent != null)
        {
            schedule.ICalendarContent = request.ICalendarContent;
        }

        if (request.AutoInactivateWhenComplete.HasValue)
        {
            schedule.AutoInactivateWhenComplete = request.AutoInactivateWhenComplete.Value;
        }

        schedule.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        var dto = MapToScheduleDto(schedule);

        logger.LogInformation(
            "Schedule updated: Id={Id}, Name={Name}",
            schedule.Id, schedule.Name);

        return Result<ScheduleDto>.Success(dto);
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        // Decode IdKey
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("Schedule", idKey));
        }

        // Find schedule
        var schedule = await context.Schedules.FindAsync([id], ct);
        if (schedule == null)
        {
            return Result.Failure(Error.NotFound("Schedule", idKey));
        }

        // Soft delete - deactivate
        schedule.IsActive = false;
        schedule.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Schedule deactivated: Id={Id}, Name={Name}", schedule.Id, schedule.Name);

        return Result.Success();
    }

    public async Task<IReadOnlyList<ScheduleOccurrenceDto>> GetOccurrencesAsync(
        string idKey,
        DateOnly? startDate = null,
        int count = 10,
        CancellationToken ct = default)
    {
        // Decode IdKey
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Array.Empty<ScheduleOccurrenceDto>();
        }

        // Find schedule
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (schedule == null)
        {
            return Array.Empty<ScheduleOccurrenceDto>();
        }

        // Limit count
        count = Math.Min(count, 52);

        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today);
        var occurrences = new List<ScheduleOccurrenceDto>();

        // For weekly schedules
        if (schedule.WeeklyDayOfWeek.HasValue && schedule.WeeklyTimeOfDay.HasValue)
        {
            var currentDate = start.ToDateTime(TimeOnly.MinValue);
            var found = 0;

            // Look forward up to 1 year
            for (int i = 0; i < 365 && found < count; i++)
            {
                if (currentDate.DayOfWeek == schedule.WeeklyDayOfWeek.Value)
                {
                    // Check if within effective date range
                    var occurrenceDate = DateOnly.FromDateTime(currentDate);
                    if (IsWithinEffectiveDateRange(schedule, occurrenceDate))
                    {
                        var occurrenceDateTime = currentDate.Add(schedule.WeeklyTimeOfDay.Value);
                        occurrences.Add(CreateOccurrenceDto(schedule, occurrenceDateTime));
                        found++;
                    }
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        return occurrences;
    }

    public async Task<ScheduleOccurrenceDto?> GetNextOccurrenceAsync(
        string idKey,
        DateTime? fromDate = null,
        CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow;
        var startDate = DateOnly.FromDateTime(from.Date);

        var occurrences = await GetOccurrencesAsync(idKey, startDate, 1, ct);
        return occurrences.FirstOrDefault();
    }

    private bool IsWithinEffectiveDateRange(Schedule schedule, DateOnly date)
    {
        if (schedule.EffectiveStartDate.HasValue && date < schedule.EffectiveStartDate.Value)
        {
            return false;
        }

        if (schedule.EffectiveEndDate.HasValue && date > schedule.EffectiveEndDate.Value)
        {
            return false;
        }

        return true;
    }

    private ScheduleOccurrenceDto CreateOccurrenceDto(Schedule schedule, DateTime occurrenceDateTime)
    {
        DateTime? checkInStart = null;
        DateTime? checkInEnd = null;
        bool isCheckInWindowOpen = false;

        if (schedule.CheckInStartOffsetMinutes.HasValue && schedule.CheckInEndOffsetMinutes.HasValue)
        {
            checkInStart = occurrenceDateTime.AddMinutes(-schedule.CheckInStartOffsetMinutes.Value);
            checkInEnd = occurrenceDateTime.AddMinutes(schedule.CheckInEndOffsetMinutes.Value);

            var now = DateTime.UtcNow;
            isCheckInWindowOpen = now >= checkInStart && now <= checkInEnd;
        }

        return new ScheduleOccurrenceDto
        {
            OccurrenceDateTime = occurrenceDateTime,
            DayOfWeekName = occurrenceDateTime.DayOfWeek.ToString(),
            FormattedTime = occurrenceDateTime.ToString("h:mm tt"),
            CheckInWindowStart = checkInStart,
            CheckInWindowEnd = checkInEnd,
            IsCheckInWindowOpen = isCheckInWindowOpen
        };
    }

    /// <summary>
    /// Maps a Schedule entity to ScheduleDto with calculated fields.
    /// </summary>
    private ScheduleDto MapToScheduleDto(Schedule schedule)
    {
        // Calculate next occurrence and check-in window
        DateTime? checkinStartTime = null;
        DateTime? checkinEndTime = null;
        bool isCheckinActive = false;

        if (schedule.WeeklyDayOfWeek.HasValue && schedule.WeeklyTimeOfDay.HasValue)
        {
            // Find the next occurrence of this schedule
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now.Date);

            // Look up to 7 days ahead for the next occurrence
            DateTime? nextOccurrence = null;
            for (int i = 0; i < 7; i++)
            {
                var checkDate = today.AddDays(i);
                var checkDateTime = checkDate.ToDateTime(TimeOnly.MinValue);

                if (checkDateTime.DayOfWeek == schedule.WeeklyDayOfWeek.Value &&
                    IsWithinEffectiveDateRange(schedule, checkDate))
                {
                    nextOccurrence = checkDateTime.Add(schedule.WeeklyTimeOfDay.Value);
                    break;
                }
            }

            // Calculate check-in window if we have an occurrence and offsets
            if (nextOccurrence.HasValue &&
                schedule.CheckInStartOffsetMinutes.HasValue &&
                schedule.CheckInEndOffsetMinutes.HasValue)
            {
                checkinStartTime = nextOccurrence.Value.AddMinutes(-schedule.CheckInStartOffsetMinutes.Value);
                checkinEndTime = nextOccurrence.Value.AddMinutes(schedule.CheckInEndOffsetMinutes.Value);

                // Check if we're currently within the check-in window
                isCheckinActive = now >= checkinStartTime && now <= checkinEndTime;
            }
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
            CheckinEndTime = checkinEndTime,
            IsPublic = schedule.IsPublic,
            Order = schedule.Order,
            EffectiveStartDate = schedule.EffectiveStartDate,
            EffectiveEndDate = schedule.EffectiveEndDate,
            ICalendarContent = schedule.ICalendarContent,
            AutoInactivateWhenComplete = schedule.AutoInactivateWhenComplete,
            CreatedDateTime = schedule.CreatedDateTime,
            ModifiedDateTime = schedule.ModifiedDateTime
        };
    }
}
