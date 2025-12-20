using Koinon.Application.DTOs.Security;
using Koinon.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Implements RBAC authorization with DENY-takes-precedence logic.
/// </summary>
public class SecurityClaimService : ISecurityClaimService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SecurityClaimService> _logger;

    public SecurityClaimService(
        IApplicationDbContext context,
        ILogger<SecurityClaimService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> HasClaimAsync(int personId, string claimType, string claimValue)
    {
        var now = DateTime.UtcNow;

        // Get active roles for the person (non-expired)
        var roleIds = await _context.PersonSecurityRoles
            .Where(psr => psr.PersonId == personId &&
                         (psr.ExpiresDateTime == null || psr.ExpiresDateTime > now))
            .Select(psr => psr.SecurityRoleId)
            .ToListAsync();

        if (!roleIds.Any())
        {
            return false;
        }

        // Get role claims for these roles matching the claim type and value
        var roleClaims = await _context.RoleSecurityClaims
            .Include(rsc => rsc.SecurityClaim)
            .Where(rsc => roleIds.Contains(rsc.SecurityRoleId) &&
                         rsc.SecurityClaim.ClaimType == claimType &&
                         rsc.SecurityClaim.ClaimValue == claimValue)
            .ToListAsync();

        // DENY takes precedence
        if (roleClaims.Any(rc => rc.AllowOrDeny == 'D'))
        {
            _logger.LogDebug(
                "Claim denied for person {PersonId}: {ClaimType}={ClaimValue}",
                personId, claimType, claimValue);
            return false;
        }

        // If any ALLOW exists, grant access
        if (roleClaims.Any(rc => rc.AllowOrDeny == 'A'))
        {
            _logger.LogDebug(
                "Claim granted for person {PersonId}: {ClaimType}={ClaimValue}",
                personId, claimType, claimValue);
            return true;
        }

        // Default deny
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyClaimAsync(int personId, string claimType, IEnumerable<string> claimValues)
    {
        var claimValuesList = claimValues.ToList();
        if (!claimValuesList.Any())
        {
            return false;
        }

        var now = DateTime.UtcNow;

        // Get active roles for the person (single query)
        var roleIds = await _context.PersonSecurityRoles
            .Where(psr => psr.PersonId == personId &&
                         (psr.ExpiresDateTime == null || psr.ExpiresDateTime > now))
            .Select(psr => psr.SecurityRoleId)
            .ToListAsync();

        if (!roleIds.Any())
        {
            return false;
        }

        // Get all role claims matching the claim type AND any of the claim values (single query)
        var roleClaims = await _context.RoleSecurityClaims
            .Include(rsc => rsc.SecurityClaim)
            .Where(rsc => roleIds.Contains(rsc.SecurityRoleId) &&
                         rsc.SecurityClaim.ClaimType == claimType &&
                         claimValuesList.Contains(rsc.SecurityClaim.ClaimValue))
            .Select(rsc => new
            {
                ClaimValue = rsc.SecurityClaim.ClaimValue,
                AllowOrDeny = rsc.AllowOrDeny
            })
            .ToListAsync();

        // Evaluate DENY-takes-precedence logic in memory
        // For each claimValue: if it has DENY -> skip it; if it has ALLOW -> return true
        foreach (var claimValue in claimValuesList)
        {
            var claimsForValue = roleClaims.Where(rc => rc.ClaimValue == claimValue).ToList();

            // Skip if any DENY exists for this claim value
            if (claimsForValue.Any(rc => rc.AllowOrDeny == 'D'))
            {
                continue;
            }

            // If any ALLOW exists without DENY, grant access
            if (claimsForValue.Any(rc => rc.AllowOrDeny == 'A'))
            {
                _logger.LogDebug(
                    "Claim granted for person {PersonId}: {ClaimType}={ClaimValue}",
                    personId, claimType, claimValue);
                return true;
            }
        }

        // No claims had ALLOW without DENY
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> HasAllClaimsAsync(int personId, string claimType, IEnumerable<string> claimValues)
    {
        var claimValuesList = claimValues.ToList();
        if (!claimValuesList.Any())
        {
            return true; // Vacuous truth: all of zero claims are satisfied
        }

        var now = DateTime.UtcNow;

        // Get active roles for the person (single query)
        var roleIds = await _context.PersonSecurityRoles
            .Where(psr => psr.PersonId == personId &&
                         (psr.ExpiresDateTime == null || psr.ExpiresDateTime > now))
            .Select(psr => psr.SecurityRoleId)
            .ToListAsync();

        if (!roleIds.Any())
        {
            return false;
        }

        // Get all role claims matching the claim type AND any of the claim values (single query)
        var roleClaims = await _context.RoleSecurityClaims
            .Include(rsc => rsc.SecurityClaim)
            .Where(rsc => roleIds.Contains(rsc.SecurityRoleId) &&
                         rsc.SecurityClaim.ClaimType == claimType &&
                         claimValuesList.Contains(rsc.SecurityClaim.ClaimValue))
            .Select(rsc => new
            {
                ClaimValue = rsc.SecurityClaim.ClaimValue,
                AllowOrDeny = rsc.AllowOrDeny
            })
            .ToListAsync();

        // Evaluate DENY-takes-precedence logic in memory
        // For EACH requested claimValue: must have ALLOW and no DENY
        foreach (var claimValue in claimValuesList)
        {
            var claimsForValue = roleClaims.Where(rc => rc.ClaimValue == claimValue).ToList();

            // If any DENY exists for this claim value, fail
            if (claimsForValue.Any(rc => rc.AllowOrDeny == 'D'))
            {
                _logger.LogDebug(
                    "Claim denied for person {PersonId}: {ClaimType}={ClaimValue}",
                    personId, claimType, claimValue);
                return false;
            }

            // If no ALLOW exists for this claim value, fail
            if (!claimsForValue.Any(rc => rc.AllowOrDeny == 'A'))
            {
                return false;
            }
        }

        // All claims have ALLOW without DENY
        _logger.LogDebug(
            "All claims granted for person {PersonId}: {ClaimType} with {Count} values",
            personId, claimType, claimValuesList.Count);
        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetPersonClaimsAsync(int personId, string claimType)
    {
        var now = DateTime.UtcNow;

        // Get active roles for the person
        var roleIds = await _context.PersonSecurityRoles
            .Where(psr => psr.PersonId == personId &&
                         (psr.ExpiresDateTime == null || psr.ExpiresDateTime > now))
            .Select(psr => psr.SecurityRoleId)
            .ToListAsync();

        if (!roleIds.Any())
        {
            return new List<string>();
        }

        // Get all role claims for this claim type
        var roleClaims = await _context.RoleSecurityClaims
            .Include(rsc => rsc.SecurityClaim)
            .Where(rsc => roleIds.Contains(rsc.SecurityRoleId) &&
                         rsc.SecurityClaim.ClaimType == claimType)
            .Select(rsc => new
            {
                ClaimValue = rsc.SecurityClaim.ClaimValue,
                AllowOrDeny = rsc.AllowOrDeny
            })
            .ToListAsync();

        // Group by claim value and apply DENY-takes-precedence logic
        var allowedClaims = roleClaims
            .GroupBy(rc => rc.ClaimValue)
            .Where(g => !g.Any(rc => rc.AllowOrDeny == 'D') && // No DENY
                        g.Any(rc => rc.AllowOrDeny == 'A'))    // At least one ALLOW
            .Select(g => g.Key)
            .ToList();

        return allowedClaims;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SecurityRoleDto>> GetPersonRolesAsync(int personId)
    {
        var now = DateTime.UtcNow;

        var roles = await _context.PersonSecurityRoles
            .Include(psr => psr.SecurityRole)
            .Where(psr => psr.PersonId == personId &&
                         (psr.ExpiresDateTime == null || psr.ExpiresDateTime > now))
            .Select(psr => new SecurityRoleDto
            {
                IdKey = psr.SecurityRole.IdKey,
                Name = psr.SecurityRole.Name,
                Description = psr.SecurityRole.Description,
                IsActive = psr.SecurityRole.IsActive,
                ExpiresDateTime = psr.ExpiresDateTime
            })
            .ToListAsync();

        return roles;
    }
}
