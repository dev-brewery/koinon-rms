using AutoMapper;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Reports;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing scheduled report generation jobs.
/// </summary>
public class ReportScheduleService(
    IApplicationDbContext context,
    IBackgroundJobService backgroundJobService,
    IReportService reportService,
    IMapper mapper,
    ILogger<ReportScheduleService> logger) : IReportScheduleService
{
    private const string JobIdPrefix = "report-schedule-";

    public async Task<PagedResult<ReportScheduleDto>> GetSchedulesAsync(
        string? reportDefinitionIdKey = null,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var query = context.ReportSchedules
            .AsNoTracking()
            .Include(rs => rs.ReportDefinition)
            .AsQueryable();

        if (reportDefinitionIdKey != null)
        {
            if (!IdKeyHelper.TryDecode(reportDefinitionIdKey, out int definitionId))
            {
                return new PagedResult<ReportScheduleDto>(
                    Array.Empty<ReportScheduleDto>(),
                    0,
                    page,
                    pageSize);
            }

            query = query.Where(rs => rs.ReportDefinitionId == definitionId);
        }

        if (!includeInactive)
        {
            query = query.Where(rs => rs.IsActive);
        }

        var totalCount = await query.CountAsync(ct);

        var schedules = await query
            .OrderBy(rs => rs.ReportDefinition.Name)
            .ThenBy(rs => rs.CronExpression)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = mapper.Map<List<ReportScheduleDto>>(schedules);

        return new PagedResult<ReportScheduleDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<ReportScheduleDto?> GetScheduleAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var schedule = await context.ReportSchedules
            .AsNoTracking()
            .Include(rs => rs.ReportDefinition)
            .FirstOrDefaultAsync(rs => rs.Id == id, ct);

        return schedule == null ? null : mapper.Map<ReportScheduleDto>(schedule);
    }

    public async Task<Result<ReportScheduleDto>> CreateScheduleAsync(
        CreateReportScheduleRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(request.ReportDefinitionIdKey, out int definitionId))
        {
            return Result<ReportScheduleDto>.Failure(
                Error.NotFound("ReportDefinition", request.ReportDefinitionIdKey));
        }

        var definition = await context.ReportDefinitions
            .FirstOrDefaultAsync(rd => rd.Id == definitionId, ct);

        if (definition == null)
        {
            return Result<ReportScheduleDto>.Failure(
                Error.NotFound("ReportDefinition", request.ReportDefinitionIdKey));
        }

        if (!definition.IsActive)
        {
            return Result<ReportScheduleDto>.Failure(
                Error.UnprocessableEntity("Cannot schedule inactive report definition"));
        }

        // Validate cron expression
        if (!IsValidCronExpression(request.CronExpression))
        {
            return Result<ReportScheduleDto>.Failure(
                Error.Validation($"Invalid cron expression: {request.CronExpression}"));
        }

        // Validate timezone
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return Result<ReportScheduleDto>.Failure(
                Error.Validation($"Invalid timezone: {request.TimeZone}"));
        }

        var schedule = mapper.Map<ReportSchedule>(request);
        schedule.ReportDefinitionId = definitionId;
        schedule.CreatedDateTime = DateTime.UtcNow;

        await context.ReportSchedules.AddAsync(schedule, ct);
        await context.SaveChangesAsync(ct);

        // Register Hangfire recurring job
        var jobId = GetJobId(schedule.IdKey);
#pragma warning disable CS4014 // Hangfire handles async methods in Expression<Action<T>>
        backgroundJobService.AddOrUpdateRecurringJob<ReportScheduleService>(
            jobId,
            s => s.TriggerScheduledReportInternalAsync(schedule.Id, CancellationToken.None),
            request.CronExpression,
            timeZone);
#pragma warning restore CS4014

        logger.LogInformation(
            "Created report schedule {ScheduleId} for definition {ReportDefinitionId} with cron: {Cron}",
            schedule.Id,
            definitionId,
            request.CronExpression);

        // Reload with navigation properties
        schedule = await context.ReportSchedules
            .Include(rs => rs.ReportDefinition)
            .FirstAsync(rs => rs.Id == schedule.Id, ct);

        var dto = mapper.Map<ReportScheduleDto>(schedule);
        return Result<ReportScheduleDto>.Success(dto);
    }

    public async Task<Result<ReportScheduleDto>> UpdateScheduleAsync(
        string idKey,
        UpdateReportScheduleRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<ReportScheduleDto>.Failure(
                Error.NotFound("ReportSchedule", idKey));
        }

        var schedule = await context.ReportSchedules
            .Include(rs => rs.ReportDefinition)
            .FirstOrDefaultAsync(rs => rs.Id == id, ct);

        if (schedule == null)
        {
            return Result<ReportScheduleDto>.Failure(
                Error.NotFound("ReportSchedule", idKey));
        }

        var cronChanged = false;
        var timeZoneChanged = false;

        // Apply updates
        if (request.CronExpression != null)
        {
            if (!IsValidCronExpression(request.CronExpression))
            {
                return Result<ReportScheduleDto>.Failure(
                    Error.Validation($"Invalid cron expression: {request.CronExpression}"));
            }
            schedule.CronExpression = request.CronExpression;
            cronChanged = true;
        }

        if (request.TimeZone != null)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return Result<ReportScheduleDto>.Failure(
                    Error.Validation($"Invalid timezone: {request.TimeZone}"));
            }
            schedule.TimeZone = request.TimeZone;
            timeZoneChanged = true;
        }

        if (request.Parameters != null) schedule.Parameters = request.Parameters;
        if (request.RecipientPersonAliasIds != null) schedule.RecipientPersonAliasIds = request.RecipientPersonAliasIds;
        if (request.OutputFormat.HasValue) schedule.OutputFormat = request.OutputFormat.Value;
        if (request.IsActive.HasValue) schedule.IsActive = request.IsActive.Value;

        schedule.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        // Update Hangfire job if cron or timezone changed
        if (cronChanged || timeZoneChanged)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone);
            var jobId = GetJobId(schedule.IdKey);

            if (schedule.IsActive)
            {
#pragma warning disable CS4014 // Hangfire handles async methods in Expression<Action<T>>
                backgroundJobService.AddOrUpdateRecurringJob<ReportScheduleService>(
                    jobId,
                    s => s.TriggerScheduledReportInternalAsync(schedule.Id, CancellationToken.None),
                    schedule.CronExpression,
                    timeZone);
#pragma warning restore CS4014

                logger.LogInformation(
                    "Updated Hangfire job for report schedule {ScheduleId}",
                    schedule.Id);
            }
            else
            {
                backgroundJobService.RemoveRecurringJob(jobId);

                logger.LogInformation(
                    "Removed Hangfire job for inactive report schedule {ScheduleId}",
                    schedule.Id);
            }
        }

        logger.LogInformation(
            "Updated report schedule {ScheduleId}",
            schedule.Id);

        var dto = mapper.Map<ReportScheduleDto>(schedule);
        return Result<ReportScheduleDto>.Success(dto);
    }

    public async Task<Result> DeleteScheduleAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("ReportSchedule", idKey));
        }

        var schedule = await context.ReportSchedules
            .FirstOrDefaultAsync(rs => rs.Id == id, ct);

        if (schedule == null)
        {
            return Result.Failure(Error.NotFound("ReportSchedule", idKey));
        }

        // Remove Hangfire recurring job
        var jobId = GetJobId(schedule.IdKey);
        backgroundJobService.RemoveRecurringJob(jobId);

        // Hard delete the schedule
        context.ReportSchedules.Remove(schedule);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Deleted report schedule {ScheduleId} and removed Hangfire job",
            schedule.Id);

        return Result.Success();
    }

    public async Task<Result<ReportRunDto>> TriggerScheduledReportAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<ReportRunDto>.Failure(
                Error.NotFound("ReportSchedule", idKey));
        }

        var schedule = await context.ReportSchedules
            .Include(rs => rs.ReportDefinition)
            .FirstOrDefaultAsync(rs => rs.Id == id, ct);

        if (schedule == null)
        {
            return Result<ReportRunDto>.Failure(
                Error.NotFound("ReportSchedule", idKey));
        }

        if (!schedule.IsActive)
        {
            return Result<ReportRunDto>.Failure(
                Error.UnprocessableEntity("Report schedule is not active"));
        }

        // Run the report using ReportService
        var runRequest = new RunReportRequest
        {
            ReportDefinitionIdKey = IdKeyHelper.Encode(schedule.ReportDefinitionId),
            Parameters = schedule.Parameters,
            OutputFormat = schedule.OutputFormat
        };

        var result = await reportService.RunReportAsync(runRequest, ct);

        if (result.IsSuccess)
        {
            // Update last run timestamp
            schedule.LastRunAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Triggered scheduled report {ScheduleId}, created run {ReportRunIdKey}",
                schedule.Id,
                result.Value!.IdKey);
        }

        return result;
    }

    /// <summary>
    /// Internal method called by Hangfire recurring job.
    /// Separated from public API to avoid exposing integer IDs.
    /// </summary>
    public async Task TriggerScheduledReportInternalAsync(
        int scheduleId,
        CancellationToken ct = default)
    {
        var schedule = await context.ReportSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(rs => rs.Id == scheduleId, ct);

        if (schedule == null)
        {
            logger.LogWarning(
                "Scheduled job triggered for non-existent report schedule {ScheduleId}",
                scheduleId);
            return;
        }

        var idKey = IdKeyHelper.Encode(scheduleId);
        _ = await TriggerScheduledReportAsync(idKey, ct);
    }

    private static string GetJobId(string scheduleIdKey) => $"{JobIdPrefix}{scheduleIdKey}";

    private static bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        // Basic validation: cron should have 5 or 6 parts
        var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length is 5 or 6;
    }
}
