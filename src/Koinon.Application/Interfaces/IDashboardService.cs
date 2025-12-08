using Koinon.Application.DTOs;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for dashboard statistics and metrics.
/// Provides aggregated data for admin dashboard displays.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves dashboard statistics including people, families, groups, check-ins, and upcoming schedules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Dashboard statistics DTO with all aggregated metrics.</returns>
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
