using System.Security.Claims;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Koinon.Api.Authorization;

/// <summary>
/// Authorization handler for claim-based access control using ISecurityClaimService.
/// </summary>
public class RequiresClaimAuthorizationHandler : AuthorizationHandler<RequiresClaimRequirement>
{
    private readonly ISecurityClaimService _securityClaimService;
    private readonly ILogger<RequiresClaimAuthorizationHandler> _logger;

    public RequiresClaimAuthorizationHandler(
        ISecurityClaimService securityClaimService,
        ILogger<RequiresClaimAuthorizationHandler> logger)
    {
        _securityClaimService = securityClaimService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequiresClaimRequirement requirement)
    {
        // Get person ID from claims
        var personIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirst("sub")
            ?? context.User.FindFirst("personId");

        if (personIdClaim == null || !int.TryParse(personIdClaim.Value, out var personId))
        {
            _logger.LogWarning("No valid person ID found in claims");
            context.Fail();
            return;
        }

        // Check if person has the required claim
        var hasClaim = await _securityClaimService.HasClaimAsync(
            personId,
            requirement.ClaimType,
            requirement.ClaimValue);

        if (hasClaim)
        {
            _logger.LogDebug(
                "Authorization succeeded for person {PersonId}: {ClaimType}={ClaimValue}",
                personId, requirement.ClaimType, requirement.ClaimValue);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Authorization failed for person {PersonId}: {ClaimType}={ClaimValue}",
                personId, requirement.ClaimType, requirement.ClaimValue);
            context.Fail();
        }
    }
}
