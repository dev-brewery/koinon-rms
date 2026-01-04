using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Exports;

/// <summary>
/// Provides family data for export operations.
/// </summary>
public class FamilyExportProvider(
    IApplicationDbContext context,
    ILogger<FamilyExportProvider> logger) : IExportDataProvider
{
    public ExportType ExportType => ExportType.Families;

    public List<ExportFieldDto> GetAvailableFields()
    {
        return
        [
            new ExportFieldDto
            {
                FieldName = "IdKey",
                DisplayName = "ID Key",
                DataType = "string",
                Description = "Unique identifier for the family",
                IsDefaultField = true,
                IsRequired = true
            },
            new ExportFieldDto
            {
                FieldName = "Name",
                DisplayName = "Family Name",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "CampusName",
                DisplayName = "Campus",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "MemberCount",
                DisplayName = "Member Count",
                DataType = "number",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "IsActive",
                DisplayName = "Is Active",
                DataType = "boolean",
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

        logger.LogInformation("Exporting family data with fields: {Fields}", string.Join(", ", fieldsToInclude));

        // Build query
        var query = context.Families
            .Include(f => f.Campus)
            .Include(f => f.Members)
            .AsNoTracking();

        // Apply filters
        if (filters != null)
        {
            if (filters.TryGetValue("campusIdKey", out var campusIdKey))
            {
                var campusId = IdKeyHelper.Decode(campusIdKey);
                query = query.Where(f => f.CampusId == campusId);
            }

            if (filters.TryGetValue("createdAfter", out var createdAfter) && DateTime.TryParse(createdAfter, out var afterDate))
            {
                query = query.Where(f => f.CreatedDateTime >= afterDate);
            }

            if (filters.TryGetValue("createdBefore", out var createdBefore) && DateTime.TryParse(createdBefore, out var beforeDate))
            {
                query = query.Where(f => f.CreatedDateTime <= beforeDate);
            }

            if (filters.TryGetValue("isActive", out var isActiveStr) && bool.TryParse(isActiveStr, out var isActive))
            {
                query = query.Where(f => f.IsActive == isActive);
            }
        }

        // Execute query
        var families = await query.ToListAsync(ct);

        logger.LogInformation("Retrieved {Count} families for export", families.Count);

        // Map to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var family in families)
        {
            var row = new Dictionary<string, object?>();

            foreach (var field in fieldsToInclude)
            {
                row[field] = field switch
                {
                    "IdKey" => IdKeyHelper.Encode(family.Id),
                    "Name" => family.Name,
                    "CampusName" => family.Campus?.Name,
                    "MemberCount" => family.Members.Count,
                    "IsActive" => family.IsActive,
                    "CreatedDateTime" => family.CreatedDateTime,
                    _ => null
                };
            }

            result.Add(row);
        }

        return result;
    }
}
