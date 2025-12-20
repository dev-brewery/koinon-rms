using Microsoft.AspNetCore.Authorization;

namespace Koinon.Api.Authorization;

/// <summary>
/// Authorization requirement for claim-based access control.
/// </summary>
public class RequiresClaimRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the claim type required (e.g., "permission").
    /// </summary>
    public string ClaimType { get; }

    /// <summary>
    /// Gets the claim value required (e.g., "person:edit").
    /// </summary>
    public string ClaimValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresClaimRequirement"/> class.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <param name="claimValue">The claim value.</param>
    public RequiresClaimRequirement(string claimType, string claimValue)
    {
        ClaimType = claimType ?? throw new ArgumentNullException(nameof(claimType));
        ClaimValue = claimValue ?? throw new ArgumentNullException(nameof(claimValue));
    }
}
