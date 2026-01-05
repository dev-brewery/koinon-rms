using System.Text.Json;
using Koinon.Application.Interfaces;
using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Reporting.DataProviders;

/// <summary>
/// Provides data for Directory reports.
/// Retrieves contact information for people with optional group and record status filtering.
/// </summary>
public class DirectoryDataProvider(
    IApplicationDbContext context,
    ILogger<DirectoryDataProvider> logger) : IReportDataProvider
{
    public ReportType ReportType => ReportType.Directory;

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        string parametersJson,
        CancellationToken ct = default)
    {
        var parameters = ParseParameters(parametersJson);

        var query = context.People
            .Include(p => p.PhoneNumbers)
            .Include(p => p.RecordStatusValue)
            .AsNoTracking();

        // Apply group filter if specified
        if (parameters.GroupId.HasValue)
        {
            query = query.Where(p => p.GroupMemberships.Any(gm => gm.GroupId == parameters.GroupId.Value));
        }

        // Apply record status filter if specified
        if (!string.IsNullOrWhiteSpace(parameters.RecordStatus))
        {
            query = query.Where(p => p.RecordStatusValue != null && p.RecordStatusValue.Value == parameters.RecordStatus);
        }

        var people = await query.ToListAsync(ct);

        // Map to dictionary rows
        var data = people
            .Select(p => new Dictionary<string, object?>
            {
                ["FirstName"] = p.FirstName,
                ["LastName"] = p.LastName,
                ["Email"] = p.Email ?? string.Empty,
                ["Phone"] = p.PhoneNumbers
                    .Where(pn => pn.NumberTypeValueId != null)
                    .OrderBy(pn => pn.NumberTypeValueId)
                    .Select(pn => pn.Number)
                    .FirstOrDefault() ?? string.Empty,
                ["RecordStatus"] = p.RecordStatusValue?.Value ?? string.Empty
            })
            .ToList();

        logger.LogInformation(
            "Retrieved {Count} people for directory report with parameters: {Parameters}",
            data.Count,
            parametersJson);

        return data;
    }

    private DirectoryParameters ParseParameters(string parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            return new DirectoryParameters();
        }

        try
        {
            return JsonSerializer.Deserialize<DirectoryParameters>(parametersJson)
                   ?? new DirectoryParameters();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse directory report parameters, using defaults");
            return new DirectoryParameters();
        }
    }

    private class DirectoryParameters
    {
        public int? GroupId { get; set; }
        public string? RecordStatus { get; set; }
    }
}
