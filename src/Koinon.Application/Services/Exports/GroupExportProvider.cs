using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Exports;

/// <summary>
/// Provides group data for export operations.
/// </summary>
public class GroupExportProvider(
    IApplicationDbContext context,
    ILogger<GroupExportProvider> logger) : IExportDataProvider
{
    public ExportType ExportType => ExportType.Groups;

    public List<ExportFieldDto> GetAvailableFields()
    {
        return
        [
            new ExportFieldDto
            {
                FieldName = "IdKey",
                DisplayName = "ID Key",
                DataType = "string",
                Description = "Unique identifier for the group",
                IsDefaultField = true,
                IsRequired = true
            },
            new ExportFieldDto
            {
                FieldName = "Name",
                DisplayName = "Group Name",
                DataType = "string",
                IsDefaultField = true
            },
            new ExportFieldDto
            {
                FieldName = "GroupTypeName",
                DisplayName = "Group Type",
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

        logger.LogInformation("Exporting group data with fields: {Fields}", string.Join(", ", fieldsToInclude));

        // Build query
        var query = context.Groups
            .Include(g => g.GroupType)
            .Include(g => g.Campus)
            .Include(g => g.Members)
            .AsNoTracking();

        // Apply filters
        if (filters != null)
        {
            if (filters.TryGetValue("groupTypeIdKey", out var groupTypeIdKey))
            {
                var groupTypeId = IdKeyHelper.Decode(groupTypeIdKey);
                query = query.Where(g => g.GroupTypeId == groupTypeId);
            }

            if (filters.TryGetValue("isActive", out var isActiveStr) && bool.TryParse(isActiveStr, out var isActive))
            {
                query = query.Where(g => g.IsActive == isActive);
            }

            if (filters.TryGetValue("campusIdKey", out var campusIdKey))
            {
                var campusId = IdKeyHelper.Decode(campusIdKey);
                query = query.Where(g => g.CampusId == campusId);
            }
        }

        // Execute query
        var groups = await query.ToListAsync(ct);

        logger.LogInformation("Retrieved {Count} groups for export", groups.Count);

        // Map to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var group in groups)
        {
            var row = new Dictionary<string, object?>();

            foreach (var field in fieldsToInclude)
            {
                row[field] = field switch
                {
                    "IdKey" => IdKeyHelper.Encode(group.Id),
                    "Name" => group.Name,
                    "GroupTypeName" => group.GroupType?.Name,
                    "CampusName" => group.Campus?.Name,
                    "MemberCount" => group.Members.Count,
                    "IsActive" => group.IsActive,
                    "CreatedDateTime" => group.CreatedDateTime,
                    _ => null
                };
            }

            result.Add(row);
        }

        return result;
    }
}
