using System.Security.Claims;
using Koinon.Application.Constants;
using Koinon.Application.Interfaces;

namespace Koinon.Api.Services;

/// <summary>
/// Implementation of IUserContext that extracts user information from JWT claims in the HTTP context.
/// </summary>
public class HttpContextUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private const string PersonIdClaimType = "person_id";
    private const string OrganizationIdClaimType = "org_id";

    public int? CurrentPersonId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(PersonIdClaimType);
            return claim != null && int.TryParse(claim.Value, out var personId)
                ? personId
                : null;
        }
    }

    public int? CurrentOrganizationId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(OrganizationIdClaimType);
            return claim != null && int.TryParse(claim.Value, out var orgId)
                ? orgId
                : null;
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? ClientIpAddress => 
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? ClientUserAgent => 
        httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

    public bool IsInRole(string role)
    {
        return httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
    }

    public bool CanAccessPerson(int personId)
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        // User can always access their own data
        if (CurrentPersonId == personId)
        {
            return true;
        }

        // Admin role has access to all person data
        if (IsInRole(Roles.Admin))
        {
            return true;
        }

        // Staff role has access to all person data
        if (IsInRole(Roles.Staff))
        {
            return true;
        }

        // Check if user has explicit permission claim for this person
        var permissionClaim = httpContextAccessor.HttpContext?.User
            .FindFirst($"can_access_person_{personId}");
        if (permissionClaim != null)
        {
            return true;
        }

        // Future: Check family relationship
        // This would require a database lookup to verify family connections
        return false;
    }

    public bool CanAccessLocation(int locationId)
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        // Admin role has access to all locations
        if (IsInRole(Roles.Admin))
        {
            return true;
        }

        // Check-in worker role
        if (IsInRole(Roles.CheckInWorker))
        {
            return true;
        }

        // Check if user has explicit permission claim for this location
        var permissionClaim = httpContextAccessor.HttpContext?.User
            .FindFirst($"can_access_location_{locationId}");
        if (permissionClaim != null)
        {
            return true;
        }

        // Future: Check group membership for location-specific access
        return false;
    }

    public bool CanAccessFamily(int familyId)
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        // Admin role has access to all families
        if (IsInRole(Roles.Admin))
        {
            return true;
        }

        // Staff role has access to all families
        if (IsInRole(Roles.Staff))
        {
            return true;
        }

        // Check if user has explicit permission claim for this family
        var permissionClaim = httpContextAccessor.HttpContext?.User
            .FindFirst($"can_access_family_{familyId}");
        if (permissionClaim != null)
        {
            return true;
        }

        // Family membership check requires database lookup
        // This is handled in the service layer (FamilyService)
        return false;
    }
}
