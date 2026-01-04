using System.Text.Json;
using AutoMapper;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Exports;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExportFieldDto = Koinon.Application.DTOs.ExportFieldDto;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing data export jobs and executing export operations.
/// </summary>
public class DataExportService(
    IApplicationDbContext context,
    IMapper mapper,
    IBackgroundJobService backgroundJobService,
    IFileStorageService fileStorageService,
    IEnumerable<IExportDataProvider> exportProviders,
    IEnumerable<IExportFormatGenerator> formatGenerators,
    ILogger<DataExportService> logger) : IDataExportService
{
    private readonly Dictionary<ExportType, IExportDataProvider> _exportProviders =
        exportProviders.ToDictionary(p => p.ExportType);

    private readonly Dictionary<ReportOutputFormat, IExportFormatGenerator> _formatGenerators =
        formatGenerators.ToDictionary(g => g.OutputFormat);

    public async Task<PagedResult<ExportJobDto>> GetExportJobsAsync(
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var query = context.ExportJobs
            .AsNoTracking()
            .Include(ej => ej.OutputFile)
            .Include(ej => ej.RequestedByPersonAlias)
                .ThenInclude(pa => pa!.Person);

        var totalCount = await query.CountAsync(ct);

        var exportJobs = await query
            .OrderByDescending(ej => ej.CreatedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = mapper.Map<List<ExportJobDto>>(exportJobs);

        return new PagedResult<ExportJobDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<ExportJobDto?> GetExportJobAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var exportJob = await context.ExportJobs
            .AsNoTracking()
            .Include(ej => ej.OutputFile)
            .Include(ej => ej.RequestedByPersonAlias)
                .ThenInclude(pa => pa!.Person)
            .FirstOrDefaultAsync(ej => ej.Id == id, ct);

        return exportJob == null ? null : mapper.Map<ExportJobDto>(exportJob);
    }

    public async Task<Result<ExportJobDto>> StartExportAsync(
        StartExportRequest request,
        CancellationToken ct = default)
    {
        // Build parameters JSON from request
        var parameters = new
        {
            fields = request.Fields,
            filters = request.Filters
        };

        var parametersJson = JsonSerializer.Serialize(parameters);

        // Create export job with Pending status
        var exportJob = new ExportJob
        {
            ExportType = request.ExportType,
            EntityType = request.EntityType,
            Status = ReportStatus.Pending,
            OutputFormat = request.OutputFormat,
            Parameters = parametersJson,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.ExportJobs.AddAsync(exportJob, ct);
        await context.SaveChangesAsync(ct);

        // Enqueue background job
        // NOTE: Hangfire correctly handles async methods in expression trees (Expression<Action<T>>).
        // The Async suffix in ProcessExportJobAsync is intentional and follows naming conventions.
        // Hangfire will await the Task returned by the method when executing the job.
#pragma warning disable CS4014 // Hangfire handles async methods in Expression<Action<T>>
        var jobId = backgroundJobService.Enqueue<IDataExportService>(s =>
            s.ProcessExportJobAsync(exportJob.Id, CancellationToken.None));
#pragma warning restore CS4014

        logger.LogInformation(
            "Queued export job {ExportJobId} for type {ExportType}",
            exportJob.Id,
            exportJob.ExportType);

        // Reload with navigation properties for DTO mapping
        exportJob = await context.ExportJobs
            .Include(ej => ej.OutputFile)
            .Include(ej => ej.RequestedByPersonAlias)
                .ThenInclude(pa => pa!.Person)
            .FirstAsync(ej => ej.Id == exportJob.Id, ct);

        var dto = mapper.Map<ExportJobDto>(exportJob);
        return Result<ExportJobDto>.Success(dto);
    }

    public async Task ProcessExportJobAsync(
        int exportJobId,
        CancellationToken ct = default)
    {
        var exportJob = await context.ExportJobs
            .FirstOrDefaultAsync(ej => ej.Id == exportJobId, ct);

        if (exportJob == null)
        {
            logger.LogError("Export job {ExportJobId} not found", exportJobId);
            return;
        }

        try
        {
            // Update status to Processing
            exportJob.Status = ReportStatus.Processing;
            exportJob.StartedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Processing export job {ExportJobId} for type {ExportType}",
                exportJobId,
                exportJob.ExportType);

            // Get the appropriate data provider
            if (!_exportProviders.TryGetValue(exportJob.ExportType, out var provider))
            {
                throw new InvalidOperationException(
                    $"No data provider registered for export type: {exportJob.ExportType}");
            }

            // Get the appropriate format generator
            if (!_formatGenerators.TryGetValue(exportJob.OutputFormat, out var generator))
            {
                throw new InvalidOperationException(
                    $"No format generator registered for output format: {exportJob.OutputFormat}");
            }

            // Parse parameters to get fields and filters
            ExportParameters? parameters = null;
            if (!string.IsNullOrEmpty(exportJob.Parameters))
            {
                parameters = JsonSerializer.Deserialize<ExportParameters>(exportJob.Parameters);
            }

            var fields = parameters?.Fields;
            var filters = parameters?.Filters;

            // Get data from provider
            logger.LogInformation(
                "Retrieving data for export job {ExportJobId}",
                exportJobId);

            var data = await provider.GetDataAsync(fields, filters, ct);

            logger.LogInformation(
                "Retrieved {RecordCount} records for export job {ExportJobId}",
                data.Count,
                exportJobId);

            // Determine which fields to include
            var fieldNames = fields ?? provider.GetAvailableFields()
                .Where(f => f.IsDefaultField)
                .Select(f => f.FieldName)
                .ToList();

            // Generate the export file
            var exportName = $"{exportJob.ExportType}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var stream = await generator.GenerateAsync(data, fieldNames, exportName, ct);

            var fileName = $"{exportName}{generator.GetFileExtension()}";
            var mimeType = generator.GetMimeType();
            var streamLength = stream.Length;

            // Reset stream position before storing
            stream.Position = 0;

            // Store the generated file
            var storageKey = await fileStorageService.StoreFileAsync(
                stream,
                fileName,
                mimeType,
                ct);

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

            // Update export job with output file and Completed status
            exportJob.OutputFileId = binaryFile.Id;
            exportJob.Status = ReportStatus.Completed;
            exportJob.CompletedAt = DateTime.UtcNow;
            exportJob.RecordCount = data.Count;
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Completed export job {ExportJobId}, output file: {BinaryFileId}, records: {RecordCount}",
                exportJobId,
                binaryFile.Id,
                data.Count);
        }
        catch (Exception ex)
        {
            // Update status to Failed with error message
            exportJob.Status = ReportStatus.Failed;
            exportJob.ErrorMessage = ex.Message.Length > 2000
                ? ex.Message[..2000]
                : ex.Message;
            exportJob.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            logger.LogError(
                ex,
                "Failed to process export job {ExportJobId}",
                exportJobId);

            throw; // Re-throw so Hangfire can track the failure
        }
    }

    public async Task<(Stream Stream, string FileName, string MimeType)?> DownloadExportAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        var exportJob = await context.ExportJobs
            .AsNoTracking()
            .Include(ej => ej.OutputFile)
            .FirstOrDefaultAsync(ej => ej.Id == id, ct);

        if (exportJob?.OutputFile == null)
        {
            return null;
        }

        var stream = await fileStorageService.GetFileAsync(exportJob.OutputFile.StorageKey, ct);
        if (stream == null)
        {
            logger.LogWarning(
                "Export output file not found in storage: {StorageKey}",
                exportJob.OutputFile.StorageKey);
            return null;
        }

        return (stream, exportJob.OutputFile.FileName, exportJob.OutputFile.MimeType);
    }

    public List<ExportFieldDto> GetAvailableFields(ExportType exportType)
    {
        if (_exportProviders.TryGetValue(exportType, out var provider))
        {
            return provider.GetAvailableFields();
        }

        return new List<ExportFieldDto>();
    }

    /// <summary>
    /// Internal class to deserialize export parameters from JSON.
    /// </summary>
    private class ExportParameters
    {
        public List<string>? Fields { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }
}
