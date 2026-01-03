using Microsoft.AspNetCore.Authorization;

namespace Koinon.Api.Authorization;

/// <summary>
/// Custom authorization attribute for claim-based access control.
/// Specifies that the endpoint requires a specific claim value.
/// </summary>
public class RequiresClaimAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresClaimAttribute"/> class.
    /// </summary>
    /// <param name="claimType">The claim type (e.g., "financial", "person").</param>
    /// <param name="claimValue">The required claim value (e.g., "view", "edit").</param>
    public RequiresClaimAttribute(string claimType, string claimValue)
    {
        ClaimType = claimType ?? throw new ArgumentNullException(nameof(claimType));
        ClaimValue = claimValue ?? throw new ArgumentNullException(nameof(claimValue));
        Policy = $"permission:{ClaimType}.{ClaimValue}";
    }

    /// <summary>
    /// Gets the claim type required.
    /// </summary>
    public string ClaimType { get; }

    /// <summary>
    /// Gets the claim value required.
    /// </summary>
    public string ClaimValue { get; }
}
