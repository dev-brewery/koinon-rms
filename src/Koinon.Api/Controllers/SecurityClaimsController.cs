using Koinon.Api.Authorization;
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
            return Problem(
                detail: result.Error?.Message ?? "Failed to load security claims",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Error");
        }

        return Ok(new { data = result.Value });
    }
}
