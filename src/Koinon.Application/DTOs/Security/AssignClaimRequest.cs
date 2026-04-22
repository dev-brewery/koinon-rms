namespace Koinon.Application.DTOs.Security;

/// <summary>
/// Request payload to associate a security claim with a role,
/// specifying whether the claim is allowed or denied for the role.
/// </summary>
public record AssignClaimRequest
{
    /// <summary>
    /// Gets the encoded identifier of the security claim to attach.
    /// </summary>
    public required string ClaimIdKey { get; init; }

    /// <summary>
    /// Gets the decision for this claim on the role: 'A' (Allow) or 'D' (Deny).
    /// </summary>
    public required char AllowOrDeny { get; init; }
}
