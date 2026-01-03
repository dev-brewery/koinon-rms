using Koinon.Application.Interfaces;
using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Reporting;

/// <summary>
/// Factory for creating report generator instances based on report type and output format.
/// </summary>
public class ReportGeneratorFactory(IServiceProvider serviceProvider)
{
    /// <summary>
    /// Gets the appropriate report generator for the specified type and format.
    /// </summary>
    /// <param name="type">Type of report to generate</param>
    /// <param name="format">Output format for the report</param>
    /// <returns>Configured report generator instance</returns>
    /// <exception cref="NotSupportedException">Thrown when the report type is not supported</exception>
    public IReportGenerator GetGenerator(ReportType type, ReportOutputFormat format)
    {
        var context = serviceProvider.GetRequiredService<IApplicationDbContext>();
        
        IReportGenerator generator = type switch
        {
            ReportType.AttendanceSummary => new AttendanceSummaryReportGenerator(
                context,
                serviceProvider.GetRequiredService<ILogger<AttendanceSummaryReportGenerator>>())
            {
                OutputFormat = format
            },
            ReportType.GivingSummary => new GivingSummaryReportGenerator(
                context,
                serviceProvider.GetRequiredService<ILogger<GivingSummaryReportGenerator>>())
            {
                OutputFormat = format
            },
            ReportType.Directory => new DirectoryReportGenerator(
                context,
                serviceProvider.GetRequiredService<ILogger<DirectoryReportGenerator>>())
            {
                OutputFormat = format
            },
            ReportType.Custom => throw new NotSupportedException("Custom reports require a custom generator implementation."),
            _ => throw new NotSupportedException($"Report type {type} is not supported.")
        };

        return generator;
    }
}
