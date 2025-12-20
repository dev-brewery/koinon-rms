using System.Globalization;
using System.Text;
using System.Text.Json;
using Koinon.Application.Common;
using Koinon.Application.Constants;
using Koinon.Application.DTOs.Import;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing data import templates and executing CSV imports.
/// </summary>
public class DataImportService(
    IApplicationDbContext context,
    ICsvParserService csvParser,
    IPersonService personService,

    ILogger<DataImportService> logger) : IDataImportService
{
    private const int BatchSize = 100;

    // Template management

    public async Task<Result<ImportTemplateDto>> CreateTemplateAsync(
        CreateImportTemplateRequest request,
        CancellationToken ct = default)
    {
        // Parse import type
        if (!Enum.TryParse<ImportType>(request.ImportType, ignoreCase: true, out var importType))
        {
            return Result<ImportTemplateDto>.Failure(new Error(
                "VALIDATION_ERROR",
                $"Invalid import type: {request.ImportType}. Valid values: {string.Join(", ", Enum.GetNames<ImportType>())}"));
        }

        // Serialize field mappings to JSON
        var fieldMappingsJson = JsonSerializer.Serialize(request.FieldMappings);

        var template = new ImportTemplate
        {
            Name = request.Name,
            Description = request.Description,
            ImportType = importType,
            FieldMappings = fieldMappingsJson,
            IsActive = true,
            IsSystem = false,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.ImportTemplates.AddAsync(template, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created import template {TemplateId}: {Name}", template.Id, template.Name);

        var dto = MapTemplateToDto(template, request.FieldMappings);
        return Result<ImportTemplateDto>.Success(dto);
    }

    public async Task<IReadOnlyList<ImportTemplateDto>> GetTemplatesAsync(
        ImportType type,
        CancellationToken ct = default)
    {
        var templates = await context.ImportTemplates
            .AsNoTracking()
            .Where(t => t.ImportType == type && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return templates.Select(t =>
        {
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(t.FieldMappings)
                ?? new Dictionary<string, string>();
            return MapTemplateToDto(t, mappings);
        }).ToList();
    }

    public async Task<Result<ImportTemplateDto>> GetTemplateAsync(
        string templateIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(templateIdKey, out int id))
        {
            return Result<ImportTemplateDto>.Failure(Error.NotFound("ImportTemplate", templateIdKey));
        }

        var template = await context.ImportTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template == null)
        {
            return Result<ImportTemplateDto>.Failure(Error.NotFound("ImportTemplate", templateIdKey));
        }

        var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(template.FieldMappings)
            ?? new Dictionary<string, string>();

        var dto = MapTemplateToDto(template, mappings);
        return Result<ImportTemplateDto>.Success(dto);
    }

    public async Task<Result> DeleteTemplateAsync(
        string templateIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(templateIdKey, out int id))
        {
            return Result.Failure(Error.NotFound("ImportTemplate", templateIdKey));
        }

        var template = await context.ImportTemplates.FindAsync(new object[] { id }, ct);

        if (template == null)
        {
            return Result.Failure(Error.NotFound("ImportTemplate", templateIdKey));
        }

        if (template.IsSystem)
        {
            return Result.Failure(Error.Forbidden("Cannot delete system templates"));
        }

        // Soft delete
        template.IsActive = false;
        template.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted (soft) import template {TemplateId}: {Name}", template.Id, template.Name);

        return Result.Success();
    }

    // Import execution

    public async Task<Result<ImportJobDto>> ValidateMappingsAsync(
        ValidateImportRequest request,
        CancellationToken ct = default)
    {
        // Parse import type
        if (!Enum.TryParse<ImportType>(request.ImportType, ignoreCase: true, out var importType))
        {
            return Result<ImportJobDto>.Failure(new Error(
                "VALIDATION_ERROR",
                $"Invalid import type: {request.ImportType}"));
        }

        // Get required fields for this import type
        var requiredFields = GetRequiredFields(importType);

        // Validate that all required fields are mapped
        var missingFields = requiredFields
            .Where(rf => !request.FieldMappings.ContainsKey(rf))
            .ToList();

        var errors = new List<ImportRowError>();

        if (missingFields.Count > 0)
        {
            errors.Add(new ImportRowError
            {
                Row = 0,
                Column = string.Join(", ", missingFields),
                Value = string.Empty,
                Message = $"Required fields not mapped: {string.Join(", ", missingFields)}"
            });
        }

        // Validate CSV file structure
        var csvPreview = await csvParser.GeneratePreviewAsync(request.FileStream, ct);

        // Validate that mapped CSV columns exist in the file
        var missingColumns = request.FieldMappings.Values
            .Where(csvColumn => !csvPreview.Headers.Contains(csvColumn, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingColumns.Count > 0)
        {
            errors.Add(new ImportRowError
            {
                Row = 0,
                Column = string.Join(", ", missingColumns),
                Value = string.Empty,
                Message = $"Mapped columns not found in CSV: {string.Join(", ", missingColumns)}"
            });
        }

        // Create pending job with validation results
        var job = new ImportJob
        {
            ImportType = importType,
            Status = errors.Count > 0 ? ImportJobStatus.Failed : ImportJobStatus.Pending,
            FileName = request.FileName,
            TotalRows = csvPreview.TotalRowCount,
            ProcessedRows = 0,
            SuccessCount = 0,
            ErrorCount = errors.Count,
            ErrorDetails = errors.Count > 0 ? SerializeErrors(errors) : null,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.ImportJobs.AddAsync(job, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Validated import mappings for {FileName}: {ErrorCount} errors",
            request.FileName,
            errors.Count);

        var dto = MapJobToDto(job, errors.Count > 0 ? errors : null);

        return errors.Count > 0
            ? Result<ImportJobDto>.Failure(new Error("VALIDATION_ERROR", "Import validation failed", null))
            : Result<ImportJobDto>.Success(dto);
    }

    public async Task<Result<ImportJobDto>> StartImportAsync(
        StartImportRequest request,
        CancellationToken ct = default)
    {
        // Parse import type
        if (!Enum.TryParse<ImportType>(request.ImportType, ignoreCase: true, out var importType))
        {
            return Result<ImportJobDto>.Failure(new Error(
                "VALIDATION_ERROR",
                $"Invalid import type: {request.ImportType}"));
        }

        // Validate mappings first
        var requiredFields = GetRequiredFields(importType);
        var missingFields = requiredFields
            .Where(rf => !request.FieldMappings.ContainsKey(rf))
            .ToList();

        if (missingFields.Count > 0)
        {
            return Result<ImportJobDto>.Failure(new Error(
                "VALIDATION_ERROR",
                $"Required fields not mapped: {string.Join(", ", missingFields)}"));
        }

        // Get CSV preview for row count
        var csvPreview = await csvParser.GeneratePreviewAsync(request.FileStream, ct);

        // Reset stream position for processing
        if (request.FileStream.CanSeek)
        {
            request.FileStream.Position = 0;
        }
        else
        {
            return Result<ImportJobDto>.Failure(new Error(
                "VALIDATION_ERROR",
                "Stream must be seekable for import processing"));
        }

        // Decode template ID if provided
        int? templateId = null;
        if (!string.IsNullOrWhiteSpace(request.ImportTemplateIdKey))
        {
            if (!IdKeyHelper.TryDecode(request.ImportTemplateIdKey, out int decodedTemplateId))
            {
                return Result<ImportJobDto>.Failure(new Error(
                    "VALIDATION_ERROR",
                    $"Invalid template IdKey: {request.ImportTemplateIdKey}"));
            }
            templateId = decodedTemplateId;
        }

        // Create import job
        var job = new ImportJob
        {
            ImportTemplateId = templateId,
            ImportType = importType,
            Status = ImportJobStatus.Pending,
            FileName = request.FileName,
            TotalRows = csvPreview.TotalRowCount,
            ProcessedRows = 0,
            SuccessCount = 0,
            ErrorCount = 0,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.ImportJobs.AddAsync(job, ct);
        await context.SaveChangesAsync(ct);

        // Start processing
        job.Status = ImportJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Starting import job {JobId} for {FileName}: {TotalRows} rows",
            job.Id,
            request.FileName,
            csvPreview.TotalRowCount);

        var errors = new List<ImportRowError>();

        try
        {
            // Process based on import type
            switch (importType)
            {
                case ImportType.People:
                    await ProcessPeopleImportAsync(job, request.FileStream, request.FieldMappings, errors, ct);
                    break;
                default:
                    errors.Add(new ImportRowError
                    {
                        Row = 0,
                        Column = "ImportType",
                        Value = importType.ToString(),
                        Message = $"Import type {importType} is not yet implemented"
                    });
                    job.ErrorCount = 1;
                    break;
            }

            // Update job completion status
            job.Status = errors.Count > 0 && job.SuccessCount == 0
                ? ImportJobStatus.Failed
                : ImportJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorDetails = errors.Count > 0 ? SerializeErrors(errors) : null;

            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Completed import job {JobId}: {SuccessCount} succeeded, {ErrorCount} failed",
                job.Id,
                job.SuccessCount,
                job.ErrorCount);

            var dto = MapJobToDto(job, errors.Count > 0 ? errors : null);
            return Result<ImportJobDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import job {JobId} failed with exception", job.Id);

            job.Status = ImportJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorDetails = SerializeErrors(new List<ImportRowError>
            {
                new()
                {
                    Row = 0,
                    Column = "System",
                    Value = string.Empty,
                    Message = $"Import failed: {ex.Message}"
                }
            });

            await context.SaveChangesAsync(ct);

            return Result<ImportJobDto>.Failure(new Error(
                "IMPORT_FAILED",
                $"Import failed: {ex.Message}"));
        }
    }

    public async Task<ImportJobDto?> GetImportStatusAsync(
        string jobIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(jobIdKey, out int id))
        {
            return null;
        }

        var job = await context.ImportJobs
            .AsNoTracking()
            .Include(j => j.ImportTemplate)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job == null)
        {
            return null;
        }

        var errors = !string.IsNullOrWhiteSpace(job.ErrorDetails)
            ? DeserializeErrors(job.ErrorDetails)
            : null;

        return MapJobToDto(job, errors);
    }

    public async Task<Result<Stream>> GenerateErrorReportAsync(
        string jobIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(jobIdKey, out int id))
        {
            return Result<Stream>.Failure(new Error(
                "VALIDATION_ERROR",
                $"Invalid job IdKey: {jobIdKey}"));
        }

        var job = await context.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job == null)
        {
            return Result<Stream>.Failure(Error.NotFound("ImportJob", jobIdKey));
        }

        var errors = !string.IsNullOrWhiteSpace(job.ErrorDetails)
            ? DeserializeErrors(job.ErrorDetails)
            : new List<ImportRowError>();

        // Generate CSV
        var csv = new StringBuilder();
        csv.AppendLine("Row,Column,Value,Error");

        foreach (var error in errors)
        {
            csv.AppendLine($"{error.Row},\"{error.Column}\",\"{EscapeCsv(error.Value)}\",\"{EscapeCsv(error.Message)}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return Result<Stream>.Success(new MemoryStream(bytes));
    }

    // Private helper methods

    private async Task ProcessPeopleImportAsync(
        ImportJob job,
        Stream fileStream,
        Dictionary<string, string> fieldMappings,
        List<ImportRowError> errors,
        CancellationToken ct)
    {
        var rows = csvParser.StreamRowsAsync(fileStream, ct);
        var batch = new List<Dictionary<string, string>>();
        var rowNumber = 1; // Header is row 0

        await foreach (var row in rows.WithCancellation(ct))
        {
            batch.Add(row);
            rowNumber++;

            if (batch.Count >= BatchSize)
            {
                await ProcessPeopleBatchAsync(job, batch, fieldMappings, errors, rowNumber - batch.Count, ct);
                batch.Clear();
            }
        }

        // Process remaining rows
        if (batch.Count > 0)
        {
            await ProcessPeopleBatchAsync(job, batch, fieldMappings, errors, rowNumber - batch.Count, ct);
        }
    }

    private async Task ProcessPeopleBatchAsync(
        ImportJob job,
        List<Dictionary<string, string>> batch,
        Dictionary<string, string> fieldMappings,
        List<ImportRowError> errors,
        int startRowNumber,
        CancellationToken ct)
    {
        using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var currentRow = startRowNumber;
            var batchSuccessCount = 0;
            var batchErrorCount = 0;
            var batchErrors = new List<ImportRowError>();

            foreach (var row in batch)
            {
                currentRow++;

                try
                {
                    var personRequest = MapRowToPersonRequest(row, fieldMappings, currentRow, batchErrors);

                    if (personRequest != null)
                    {
                        var result = await personService.CreateAsync(personRequest, ct);

                        if (result.IsSuccess)
                        {
                            batchSuccessCount++;
                        }
                        else
                        {
                            batchErrorCount++;
                            batchErrors.Add(new ImportRowError
                            {
                                Row = currentRow,
                                Column = "Person",
                                Value = $"{personRequest.FirstName} {personRequest.LastName}",
                                Message = result.Error?.Message ?? "Unknown error"
                            });
                        }
                    }
                    else
                    {
                        batchErrorCount++;
                        // Error already added in MapRowToPersonRequest
                    }
                }
                catch (Exception ex)
                {
                    batchErrorCount++;
                    batchErrors.Add(new ImportRowError
                    {
                        Row = currentRow,
                        Column = "System",
                        Value = string.Empty,
                        Message = $"Error processing row: {ex.Message}"
                    });
                }
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // Only apply counts after successful commit
            job.SuccessCount += batchSuccessCount;
            job.ErrorCount += batchErrorCount;
            errors.AddRange(batchErrors);
            job.ProcessedRows += batch.Count;
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "Processed batch for import job {JobId}: {ProcessedRows}/{TotalRows}",
                job.Id,
                job.ProcessedRows,
                job.TotalRows);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);

            logger.LogError(ex, "Batch processing failed for import job {JobId}, rolling back", job.Id);

            // Entire batch failed - don't use aborted in-loop counters
            job.ProcessedRows += batch.Count;
            job.ErrorCount += batch.Count;
            errors.Add(new ImportRowError
            {
                Row = startRowNumber,
                Column = "Batch",
                Value = string.Empty,
                Message = $"Batch processing failed: {ex.Message}"
            });
        }
    }

    private CreatePersonRequest? MapRowToPersonRequest(
        Dictionary<string, string> row,
        Dictionary<string, string> fieldMappings,
        int rowNumber,
        List<ImportRowError> errors)
    {
        // Get mapped value from CSV row
        string GetMappedValue(string fieldName)
        {
            if (!fieldMappings.TryGetValue(fieldName, out var csvColumn))
            {
                return string.Empty;
            }

            return row.TryGetValue(csvColumn, out var value) ? value.Trim() : string.Empty;
        }

        // Validate required fields
        var firstName = GetMappedValue("FirstName");
        var lastName = GetMappedValue("LastName");

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add(new ImportRowError
            {
                Row = rowNumber,
                Column = "FirstName",
                Value = firstName,
                Message = "FirstName is required"
            });
            return null;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add(new ImportRowError
            {
                Row = rowNumber,
                Column = "LastName",
                Value = lastName,
                Message = "LastName is required"
            });
            return null;
        }

        // Parse optional fields
        var email = GetMappedValue("Email");
        var nickName = GetMappedValue("NickName");
        var middleName = GetMappedValue("MiddleName");
        var genderStr = GetMappedValue("Gender");
        var birthDateStr = GetMappedValue("BirthDate");
        var mobilePhone = GetMappedValue("MobilePhone");
        var connectionStatusId = GetMappedValue("ConnectionStatusValueId");
        var recordStatusId = GetMappedValue("RecordStatusValueId");
        var campusId = GetMappedValue("CampusId");

        // Parse gender
        string? gender = null;
        if (!string.IsNullOrWhiteSpace(genderStr))
        {
            gender = genderStr.ToUpperInvariant() switch
            {
                "M" or "MALE" => GenderValues.Male,
                "F" or "FEMALE" => GenderValues.Female,
                _ => GenderValues.Unknown
            };
        }

        // Parse birth date
        DateOnly? birthDate = null;
        if (!string.IsNullOrWhiteSpace(birthDateStr))
        {
            if (TryParseDateOnly(birthDateStr, out var parsed))
            {
                birthDate = parsed;
            }
            else
            {
                errors.Add(new ImportRowError
                {
                    Row = rowNumber,
                    Column = "BirthDate",
                    Value = birthDateStr,
                    Message = "Invalid date format (expected yyyy-MM-dd, MM/dd/yyyy, or dd/MM/yyyy)"
                });
            }
        }

        // Build phone numbers list
        List<CreatePhoneNumberRequest>? phoneNumbers = null;
        if (!string.IsNullOrWhiteSpace(mobilePhone))
        {
            phoneNumbers = new List<CreatePhoneNumberRequest>
            {
                new()
                {
                    Number = NormalizePhone(mobilePhone),
                    IsMessagingEnabled = true
                }
            };
        }

        return new CreatePersonRequest
        {
            FirstName = firstName,
            LastName = lastName,
            NickName = string.IsNullOrWhiteSpace(nickName) ? null : nickName,
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Gender = gender,
            BirthDate = birthDate,
            ConnectionStatusValueId = string.IsNullOrWhiteSpace(connectionStatusId) ? null : connectionStatusId,
            RecordStatusValueId = string.IsNullOrWhiteSpace(recordStatusId) ? null : recordStatusId,
            CampusId = string.IsNullOrWhiteSpace(campusId) ? null : campusId,
            PhoneNumbers = phoneNumbers
        };
    }

    private static string[] GetRequiredFields(ImportType importType)
    {
        return importType switch
        {
            ImportType.People => new[] { "FirstName", "LastName" },
            _ => Array.Empty<string>()
        };
    }

    private static ImportTemplateDto MapTemplateToDto(ImportTemplate template, Dictionary<string, string> mappings)
    {
        return new ImportTemplateDto
        {
            IdKey = template.IdKey,
            Guid = template.Guid,
            Name = template.Name,
            Description = template.Description,
            ImportType = template.ImportType.ToString(),
            FieldMappings = mappings,
            IsActive = template.IsActive,
            IsSystem = template.IsSystem,
            CreatedDateTime = template.CreatedDateTime,
            ModifiedDateTime = template.ModifiedDateTime
        };
    }

    private ImportJobDto MapJobToDto(ImportJob job, List<ImportRowError>? errors)
    {
        return new ImportJobDto
        {
            IdKey = job.IdKey,
            Guid = job.Guid,
            ImportTemplateIdKey = job.ImportTemplateId.HasValue
                ? IdKeyHelper.Encode(job.ImportTemplateId.Value)
                : null,
            ImportType = job.ImportType.ToString(),
            Status = job.Status.ToString(),
            FileName = job.FileName,
            TotalRows = job.TotalRows,
            ProcessedRows = job.ProcessedRows,
            SuccessCount = job.SuccessCount,
            ErrorCount = job.ErrorCount,
            Errors = errors,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            CreatedDateTime = job.CreatedDateTime
        };
    }

    private static string SerializeErrors(List<ImportRowError> errors)
    {
        return JsonSerializer.Serialize(new { errors });
    }

    private static List<ImportRowError> DeserializeErrors(string json)
    {
        var wrapper = JsonSerializer.Deserialize<ErrorWrapper>(json);
        return wrapper?.Errors ?? new List<ImportRowError>();
    }

    private static bool TryParseDateOnly(string value, out DateOnly date)
    {
        string[] formats =
        [
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "M/d/yyyy",
            "d/M/yyyy"
        ];

        if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime);
            return true;
        }

        date = default;
        return false;
    }

    private static string NormalizePhone(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"'))
        {
            return value.Replace("\"", "\"\"");
        }
        return value;
    }

    private class ErrorWrapper
    {
        public List<ImportRowError> Errors { get; set; } = new();
    }
}
