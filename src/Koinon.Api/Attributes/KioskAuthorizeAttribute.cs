using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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

        // Get validation service from DI container
        var validationService = context.HttpContext.RequestServices
            .GetService<IDeviceValidationService>();

        if (validationService == null)
        {
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<KioskAuthorizeAttribute>>();
            logger?.LogError("IDeviceValidationService not registered in DI container");

            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Service configuration error",
                Detail = "Kiosk validation service is not available",
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.HttpContext.Request.Path
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            return;
        }

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
    }
}
