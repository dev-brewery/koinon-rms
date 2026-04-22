using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Koinon.Api.Attributes;

/// <summary>
/// Authorization filter for kiosk-based endpoints.
/// Validates X-Kiosk-Token header against registered kiosks in the database.
/// Checks that the kiosk is active and the token has not expired.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class KioskAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // In Development environment, allow anonymous kiosk access for E2E testing
        var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment() &&
            !context.HttpContext.Request.Headers.ContainsKey("X-Kiosk-Token"))
        {
            // Flag the request so service-layer auth (IUserContext.IsAuthenticated)
            // recognizes this as an authorized kiosk request without JWT
            context.HttpContext.Items["KioskBypass"] = true;
            return;
        }

        // Extract token from header
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Kiosk-Token", out var kioskToken) ||
            string.IsNullOrWhiteSpace(kioskToken))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Kiosk authentication required",
                Detail = "This endpoint requires a valid X-Kiosk-Token header",
                Status = StatusCodes.Status401Unauthorized,
                Instance = context.HttpContext.Request.Path
            });
            return;
        }

        // Get validation service from DI container (Issue #41 - fail fast if not registered)
        var validationService = context.HttpContext.RequestServices
            .GetRequiredService<IDeviceValidationService>();

        // Validate token against database
        var deviceId = await validationService.ValidateKioskTokenAsync(
            kioskToken!,
            context.HttpContext.RequestAborted);

        if (deviceId == null)
        {
            // Token is invalid, expired, or kiosk is inactive
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Invalid kiosk token",
                Detail = "The provided kiosk token is invalid, expired, or the kiosk is inactive",
                Status = StatusCodes.Status401Unauthorized,
                Instance = context.HttpContext.Request.Path
            });
            return;
        }

        // Store device ID in HttpContext for audit trail
        context.HttpContext.Items["KioskDeviceId"] = deviceId.Value;
        // Flag as kiosk-authorized so service-layer auth allows access
        context.HttpContext.Items["KioskBypass"] = true;
    }
}
