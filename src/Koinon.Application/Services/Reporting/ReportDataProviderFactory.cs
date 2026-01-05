using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Koinon.Application.Services.Reporting;

/// <summary>
/// Factory for resolving report data provider instances based on report type.
/// </summary>
public class ReportDataProviderFactory(IServiceProvider serviceProvider)
{
    /// <summary>
    /// Gets the appropriate data provider for the specified report type.
    /// </summary>
    /// <param name="type">Type of report to get data provider for</param>
    /// <returns>Data provider instance for the specified report type</returns>
    /// <exception cref="InvalidOperationException">Thrown when no provider is registered for the report type</exception>
    public IReportDataProvider GetProvider(ReportType type)
    {
        var providers = serviceProvider.GetServices<IReportDataProvider>();
        // SYNC OK: DI service resolution
        var provider = providers.FirstOrDefault(p => p.ReportType == type);

        if (provider == null)
        {
            throw new InvalidOperationException($"No data provider registered for report type {type}.");
        }

        return provider;
    }
}
