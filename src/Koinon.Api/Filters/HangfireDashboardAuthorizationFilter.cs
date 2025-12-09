using Hangfire.Dashboard;

namespace Koinon.Api.Filters;

/// <summary>
/// Authorization filter for Hangfire dashboard.
/// Requires the user to be authenticated and in the "Admin" role.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Determines whether the current user is authorized to access the Hangfire dashboard.
    /// </summary>
    /// <param name="context">The dashboard context containing user information.</param>
    /// <returns>True if the user is authenticated and has the Admin role; otherwise, false.</returns>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Require authentication
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Require Admin role
        return httpContext.User.IsInRole("Admin");
    }
}
