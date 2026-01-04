using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Exports;

/// <summary>
/// Provides person data for export operations.
/// </summary>
public class PersonExportProvider(
    IApplicationDbContext context,
    ILogger<PersonExportProvider> logger) : IExportDataProvider
{
    public ExportType ExportType => ExportType.People;

    public List<ExportFieldDto> GetAvailableFields()
    {
        return
        [
            new ExportFieldDto
            {
                FieldName = "IdKey",
                DisplayName = "ID Key",
                DataType = "string",
                Description = "Unique identifier for the person",
                IsDefaultField = true,
                IsRequired = true
            },
            new ExportFieldDto
            {
                FieldName = "FirstName",
                DisplayName = "First Name",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "LastName",
                DisplayName = "Last Name",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "NickName",
                DisplayName = "Nick Name",
                DataType = "string",
                IsDefaultField = false
            },
            new ExportFieldDto
            {
                FieldName = "Email",
                DisplayName = "Email",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "PhoneNumber",
                DisplayName = "Phone Number",
                DataType = "string",
                Description = "Primary mobile phone number",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "BirthDate",
                DisplayName = "Birth Date",
                DataType = "date",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "Gender",
                DisplayName = "Gender",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "RecordStatus",
                DisplayName = "Record Status",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "ConnectionStatus",
                DisplayName = "Connection Status",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "CreatedDateTime",
                DisplayName = "Created Date",
                DataType = "date",
                IsDefaultField = true
            }
        ];
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        List<string>? fields,
        Dictionary<string, string>? filters,
        CancellationToken ct = default)
    {
        // Determine which fields to include
        var fieldsToInclude = fields ?? GetAvailableFields()
            .Where(f => f.IsDefaultField)
            .Select(f => f.FieldName)
            .ToList();

        logger.LogInformation("Exporting person data with fields: {Fields}", string.Join(", ", fieldsToInclude));

        // Build query
        var query = context.People
            .Include(p => p.RecordStatusValue)
            .Include(p => p.ConnectionStatusValue)
            .Include(p => p.PhoneNumbers)
            .AsNoTracking();

        // Apply filters
        if (filters != null)
        {
            if (filters.TryGetValue("recordStatus", out var recordStatus))
            {
                query = query.Where(p => p.RecordStatusValue != null && p.RecordStatusValue.Value == recordStatus);
            }

            if (filters.TryGetValue("connectionStatus", out var connectionStatus))
            {
                query = query.Where(p => p.ConnectionStatusValue != null && p.ConnectionStatusValue.Value == connectionStatus);
            }

            if (filters.TryGetValue("createdAfter", out var createdAfter) && DateTime.TryParse(createdAfter, out var afterDate))
            {
                query = query.Where(p => p.CreatedDateTime >= afterDate);
            }

            if (filters.TryGetValue("createdBefore", out var createdBefore) && DateTime.TryParse(createdBefore, out var beforeDate))
            {
                query = query.Where(p => p.CreatedDateTime <= beforeDate);
            }
        }

        // Execute query
        var people = await query.ToListAsync(ct);

        logger.LogInformation("Retrieved {Count} people for export", people.Count);

        // Map to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var person in people)
        {
            var row = new Dictionary<string, object?>();

            foreach (var field in fieldsToInclude)
            {
                row[field] = field switch
                {
                    "IdKey" => IdKeyHelper.Encode(person.Id),
                    "FirstName" => person.FirstName,
                    "LastName" => person.LastName,
                    "NickName" => person.NickName,
                    "Email" => person.Email,
                    "PhoneNumber" => person.PhoneNumbers.FirstOrDefault(p => p.NumberTypeValueId == 12)?.Number, // Mobile type
                    "BirthDate" => person.BirthDate,
                    "Gender" => person.Gender.ToString(),
                    "RecordStatus" => person.RecordStatusValue?.Value,
                    "ConnectionStatus" => person.ConnectionStatusValue?.Value,
                    "CreatedDateTime" => person.CreatedDateTime,
                    _ => null
                };
            }

            result.Add(row);
        }

        return result;
    }
}
