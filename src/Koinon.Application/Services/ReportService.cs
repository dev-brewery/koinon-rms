using AutoMapper;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Reports;
using Koinon.Application.Interfaces;
using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing report definitions and executing report generation jobs.
/// </summary>
public class ReportService(
    IApplicationDbContext context,
    IBackgroundJobService backgroundJobService,
    IFileStorageService fileStorageService,
    IServiceProvider serviceProvider,
    IMapper mapper,
    ILogger<ReportService> logger) : IReportService
{
    public async Task<PagedResult<ReportDefinitionDto>> GetDefinitionsAsync(
        bool includeInactive = false,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var query = context.ReportDefinitions.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(rd => rd.IsActive);
        }

        var totalCount = await query.CountAsync(ct);

        var definitions = await query
            .OrderBy(rd => rd.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = mapper.Map<List<ReportDefinitionDto>>(definitions);

        return new PagedResult<ReportDefinitionDto>(
            dtos,
            totalCount,
            page,
            pageSize);
    }

    public async Task<ReportDefinitionDto?> GetDefinitionAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var definition = await context.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(rd => rd.Id == id, ct);

        return definition == null ? null : mapper.Map<ReportDefinitionDto>(definition);
    }

    public async Task<Result<ReportDefinitionDto>> CreateDefinitionAsync(
        CreateReportDefinitionRequest request,
        CancellationToken ct = default)
    {
        var definition = mapper.Map<ReportDefinition>(request);
        definition.CreatedDateTime = DateTime.UtcNow;

        await context.ReportDefinitions.AddAsync(definition, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created report definition {ReportDefinitionId}: {Name}",
            definition.Id,
            definition.Name);

        var dto = mapper.Map<ReportDefinitionDto>(definition);
        return Result<ReportDefinitionDto>.Success(dto);
    }

    public async Task<Result<ReportDefinitionDto>> UpdateDefinitionAsync(
        string idKey,
        UpdateReportDefinitionRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<ReportDefinitionDto>.Failure(
                Error.NotFound("ReportDefinition", idKey));
        }

        var definition = await context.ReportDefinitions
            .FirstOrDefaultAsync(rd => rd.Id == id, ct);

        if (definition == null)
        {
            return Result<ReportDefinitionDto>.Failure(
                Error.NotFound("ReportDefinition", idKey));
        }

        if (definition.IsSystem)
        {
            return Result<ReportDefinitionDto>.Failure(
                Error.UnprocessableEntity("Cannot modify system report definitions"));
        }

        // Apply updates
        if (request.Name != null)
        {
            definition.Name = request.Name;
        }

        if (request.Description != null)
        {
            definition.Description = request.Description;
        }

        if (request.ParameterSchema != null)
        {
            definition.ParameterSchema = request.ParameterSchema;
        }

        if (request.DefaultParameters != null)
        {
            definition.DefaultParameters = request.DefaultParameters;
        }

        if (request.OutputFormat.HasValue)
        {
            definition.OutputFormat = request.OutputFormat.Value;
        }

        if (request.IsActive.HasValue)
        {
            definition.IsActive = request.IsActive.Value;
        }

        definition.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated report definition {ReportDefinitionId}: {Name}",
            definition.Id,
            definition.Name);

        var dto = mapper.Map<ReportDefinitionDto>(definition);
        return Result<ReportDefinitionDto>.Success(dto);
    }

    public async Task<Result> DeleteDefinitionAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("ReportDefinition", idKey));
        }

        var definition = await context.ReportDefinitions
            .FirstOrDefaultAsync(rd => rd.Id == id, ct);

        if (definition == null)
        {
            return Result.Failure(Error.NotFound("ReportDefinition", idKey));
        }

        if (definition.IsSystem)
        {
            return Result.Failure(
                Error.UnprocessableEntity("Cannot delete system report definitions"));
        }

        // Soft delete
        definition.IsActive = false;
        definition.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Deleted (soft) report definition {ReportDefinitionId}: {Name}",
            definition.Id,
            definition.Name);

        return Result.Success();
    }

    public async Task<Result<ReportRunDto>> RunReportAsync(
        RunReportRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(request.ReportDefinitionIdKey, out int definitionId))
        {
            return Result<ReportRunDto>.Failure(
                Error.NotFound("ReportDefinition", request.ReportDefinitionIdKey));
        }

        var definition = await context.ReportDefinitions
            .FirstOrDefaultAsync(rd => rd.Id == definitionId, ct);

        if (definition == null)
        {
            return Result<ReportRunDto>.Failure(
                Error.NotFound("ReportDefinition", request.ReportDefinitionIdKey));
        }

        if (!definition.IsActive)
        {
            return Result<ReportRunDto>.Failure(
                Error.UnprocessableEntity("Report definition is not active"));
        }

        // Create report run with Pending status
        var reportRun = new ReportRun
        {
            ReportDefinitionId = definitionId,
            Status = ReportStatus.Pending,
            Parameters = request.Parameters ?? definition.DefaultParameters ?? "{}",
            CreatedDateTime = DateTime.UtcNow
        };

        await context.ReportRuns.AddAsync(reportRun, ct);
        await context.SaveChangesAsync(ct);

        // Enqueue background job
        // NOTE: Hangfire correctly handles async methods in expression trees (Expression<Action<T>>).
        // The Async suffix in ProcessReportJobAsync is intentional and follows naming conventions.
        // Hangfire will await the Task returned by the method when executing the job.
#pragma warning disable CS4014 // Hangfire handles async methods in Expression<Action<T>>
        var jobId = backgroundJobService.Enqueue<ReportService>(s =>
            s.ProcessReportJobAsync(reportRun.Id, request.OutputFormat ?? definition.OutputFormat, CancellationToken.None));
#pragma warning restore CS4014

        logger.LogInformation(
            "Queued report run {ReportRunId} for definition {ReportDefinitionId}",
            reportRun.Id,
            definitionId);

        // Reload with navigation properties for DTO mapping
        reportRun = await context.ReportRuns
            .Include(rr => rr.ReportDefinition)
            .FirstAsync(rr => rr.Id == reportRun.Id, ct);

        var dto = mapper.Map<ReportRunDto>(reportRun);
        return Result<ReportRunDto>.Success(dto);
    }

    public async Task<PagedResult<ReportRunDto>> GetRunsAsync(
        string? reportDefinitionIdKey = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var query = context.ReportRuns
            .AsNoTracking()
            .Include(rr => rr.ReportDefinition)
            .Include(rr => rr.RequestedByPersonAlias)
                .ThenInclude(pa => pa!.Person)
            .AsQueryable();

        if (reportDefinitionIdKey != null)
        {
            if (!IdKeyHelper.TryDecode(reportDefinitionIdKey, out int definitionId))
            {
                return new PagedResult<ReportRunDto>(
                    Array.Empty<ReportRunDto>(),
                    0,
                    page,
                    pageSize);
            }

            query = query.Where(rr => rr.ReportDefinitionId == definitionId);
        }

        var totalCount = await query.CountAsync(ct);

        var runs = await query
            .OrderByDescending(rr => rr.CreatedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = mapper.Map<List<ReportRunDto>>(runs);

        return new PagedResult<ReportRunDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<ReportRunDto?> GetRunAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var run = await context.ReportRuns
            .AsNoTracking()
            .Include(rr => rr.ReportDefinition)
            .Include(rr => rr.RequestedByPersonAlias)
                .ThenInclude(pa => pa!.Person)
            .FirstOrDefaultAsync(rr => rr.Id == id, ct);

        return run == null ? null : mapper.Map<ReportRunDto>(run);
    }

    public async Task<(Stream Stream, string FileName, string MimeType)?> DownloadReportAsync(
        string reportRunIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(reportRunIdKey, out int runId))
        {
            return null;
        }

        var run = await context.ReportRuns
            .AsNoTracking()
            .Include(rr => rr.OutputFile)
            .Include(rr => rr.ReportDefinition)
            .FirstOrDefaultAsync(rr => rr.Id == runId, ct);

        if (run?.OutputFile == null)
        {
            return null;
        }

        var stream = await fileStorageService.GetFileAsync(run.OutputFile.StorageKey, ct);
        if (stream == null)
        {
            logger.LogWarning(
                "Report output file not found in storage: {StorageKey}",
                run.OutputFile.StorageKey);
            return null;
        }

        return (stream, run.OutputFile.FileName, run.OutputFile.MimeType);
    }

    /// <summary>
    /// Background job method to process report generation.
    /// Called by Hangfire after RunReportAsync enqueues the job.
    /// </summary>
    public async Task ProcessReportJobAsync(
        int reportRunId,
        ReportOutputFormat outputFormat,
        CancellationToken ct = default)
    {
        var run = await context.ReportRuns
            .Include(rr => rr.ReportDefinition)
            .FirstOrDefaultAsync(rr => rr.Id == reportRunId, ct);

        if (run == null)
        {
            logger.LogError("Report run {ReportRunId} not found", reportRunId);
            return;
        }

        try
        {
            // Update status to Processing
            run.Status = ReportStatus.Processing;
            run.StartedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Processing report run {ReportRunId} for definition {ReportDefinitionId}",
                reportRunId,
                run.ReportDefinitionId);

            // Resolve the appropriate report generator based on output format
            var generator = serviceProvider
                .GetServices<IReportGenerator>()
                .FirstOrDefault(g => g.OutputFormat == outputFormat);

            if (generator == null)
            {
                throw new InvalidOperationException(
                    $"No report generator found for output format: {outputFormat}");
            }

            // Generate the report (this would call the actual report data query and generation logic)
            // For now, we're just calling the generator with empty data
            var (stream, fileName, mimeType) = await generator.GenerateAsync(
                run.ReportDefinition.Name,
                run.ReportDefinition.ReportType,
                Array.Empty<object>(), // TODO(#396): Implement report data providers
                run.Parameters,
                ct);

            // Get stream length before storing
            var streamLength = stream.Length;

            // Store the generated file
            var storageKey = await fileStorageService.StoreFileAsync(
                stream,
                fileName,
                mimeType,
                ct);

            // Dispose the stream after storage
            await stream.DisposeAsync();

            // Create BinaryFile record
            var binaryFile = new BinaryFile
            {
                FileName = fileName,
                MimeType = mimeType,
                StorageKey = storageKey,
                FileSizeBytes = streamLength,
                CreatedDateTime = DateTime.UtcNow
            };

            await context.BinaryFiles.AddAsync(binaryFile, ct);
            await context.SaveChangesAsync(ct);

            // Update report run with output file and Completed status
            run.OutputFileId = binaryFile.Id;
            run.Status = ReportStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Completed report run {ReportRunId}, output file: {BinaryFileId}",
                reportRunId,
                binaryFile.Id);
        }
        catch (Exception ex)
        {
            // Update status to Failed with error message
            run.Status = ReportStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            logger.LogError(
                ex,
                "Failed to process report run {ReportRunId}",
                reportRunId);

            throw; // Re-throw so Hangfire can track the failure
        }
    }
}
