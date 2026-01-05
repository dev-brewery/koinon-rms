using System.Text.Json;
using Koinon.Application.Interfaces;
using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Reporting.DataProviders;

/// <summary>
/// Provides data for attendance summary reports.
/// Aggregates attendance records grouped by group and date.
/// </summary>
public class AttendanceSummaryDataProvider(
    IApplicationDbContext context,
    ILogger<AttendanceSummaryDataProvider> logger) : IReportDataProvider
{
    public ReportType ReportType => ReportType.AttendanceSummary;

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        string parametersJson,
        CancellationToken ct = default)
    {
        logger.LogInformation("Retrieving attendance summary data");

        // Parse parameters
        var parameters = ParseParameters(parametersJson);

        // Query attendance data with includes
        var query = context.Attendances
            .Include(a => a.Occurrence)
            .ThenInclude(o => o!.Group)
            .AsNoTracking();

        // Apply date range filters
        if (parameters.StartDate.HasValue)
        {
            var startDateOnly = DateOnly.FromDateTime(parameters.StartDate.Value);
            query = query.Where(a => a.Occurrence!.OccurrenceDate >= startDateOnly);
        }

        if (parameters.EndDate.HasValue)
        {
            var endDateOnly = DateOnly.FromDateTime(parameters.EndDate.Value);
            query = query.Where(a => a.Occurrence!.OccurrenceDate <= endDateOnly);
        }

        // Apply group filter
        if (parameters.GroupId.HasValue)
        {
            query = query.Where(a => a.Occurrence!.GroupId == parameters.GroupId.Value);
        }

        var attendances = await query.ToListAsync(ct);

        // Group by group name and date, then aggregate
        // SYNC OK: In-memory LINQ after async query
        var summaryData = attendances
            .Where(a => a.Occurrence?.Group != null)
            .GroupBy(a => new
            {
                GroupName = a.Occurrence!.Group!.Name,
                Date = a.Occurrence.OccurrenceDate
            })
            .Select(g => new Dictionary<string, object?>
            {
                ["GroupName"] = g.Key.GroupName,
                ["Date"] = g.Key.Date,
                ["PresentCount"] = g.Count(a => a.DidAttend == true),
                ["AbsentCount"] = g.Count(a => a.DidAttend == false)
            })
            .OrderBy(d => d["Date"])
            .ThenBy(d => d["GroupName"])
            .ToList();

        logger.LogInformation(
            "Retrieved {Count} attendance summary rows for date range {StartDate} to {EndDate}",
            summaryData.Count,
            parameters.StartDate?.ToString("yyyy-MM-dd") ?? "unspecified",
            parameters.EndDate?.ToString("yyyy-MM-dd") ?? "unspecified");

        return summaryData.AsReadOnly();
    }

    private AttendanceReportParameters ParseParameters(string parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            logger.LogDebug("No parameters provided, using defaults");
            return new AttendanceReportParameters();
        }

        try
        {
            var parameters = JsonSerializer.Deserialize<AttendanceReportParameters>(
                parametersJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parameters ?? new AttendanceReportParameters();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse attendance report parameters, using defaults");
            return new AttendanceReportParameters();
        }
    }

    private class AttendanceReportParameters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? GroupId { get; set; }
    }
}
