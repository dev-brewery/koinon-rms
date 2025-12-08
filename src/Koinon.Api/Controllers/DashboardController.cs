using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for dashboard statistics and metrics.
/// Provides aggregated data for admin dashboard displays.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController(
    IDashboardService dashboardService,
    ILogger<DashboardController> logger) : ControllerBase
{
    /// <summary>
    /// Gets dashboard statistics including people, families, groups, and check-in metrics.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dashboard statistics summary</returns>
    /// <response code="200">Returns dashboard statistics</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
    {
        var stats = await dashboardService.GetStatsAsync(ct);

        logger.LogInformation(
            "Dashboard stats retrieved: People={TotalPeople}, Families={TotalFamilies}, Groups={ActiveGroups}",
            stats.TotalPeople, stats.TotalFamilies, stats.ActiveGroups);

        return Ok(stats);
    }
}
