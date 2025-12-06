using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Koinon.Api.Attributes;

/// <summary>
/// Authorization filter for kiosk-based endpoints.
/// Requires X-Kiosk-Token header to be present.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class KioskAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Kiosk-Token", out var kioskToken) ||
            string.IsNullOrWhiteSpace(kioskToken))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Kiosk authentication required",
                Detail = "This endpoint requires a valid X-Kiosk-Token header",
                Status = StatusCodes.Status401Unauthorized
            });
        }
        // TODO(#41): Validate token against registered kiosks in database
    }
}
