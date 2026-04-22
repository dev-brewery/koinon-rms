using Koinon.Api.Authorization;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Security;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Lists all available security claims for the admin role-claim picker.
/// Gated behind the "security:manage" claim.
/// </summary>
[ApiController]
[Route("api/v1/admin/security-claims")]
[Authorize]
[RequiresClaim("security", "manage")]
public class SecurityClaimsController(
    ISecurityRoleService service) : ControllerBase
{
    /// <summary>
    /// Gets all available security claims in the system.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SecurityClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct = default)
    {
        var result = await service.GetAllClaimsAsync(ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND" => NotFound(new ProblemDetails
        {
            Title = "Not Found",
            Detail = error.Message,
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path
        }),
        "VALIDATION_ERROR" => BadRequest(new ProblemDetails
        {
            Title = error.Message,
            Detail = error.Details != null
                ? string.Join("; ", error.Details.SelectMany(kvp => kvp.Value))
                : null,
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path,
            Extensions = { ["errors"] = error.Details }
        }),
        _ => UnprocessableEntity(new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = StatusCodes.Status422UnprocessableEntity,
            Instance = HttpContext.Request.Path
        })
    };
}
