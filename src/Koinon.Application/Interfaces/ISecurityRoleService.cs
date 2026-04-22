using Koinon.Application.Common;
using Koinon.Application.DTOs.Security;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Administrative service for managing security roles, their claims, and their members.
/// This is the admin CRUD surface for RBAC configuration and is separate from the
/// runtime authorization checks exposed by <see cref="ISecurityClaimService"/>.
/// </summary>
public interface ISecurityRoleService
{
    /// <summary>
    /// Gets all security roles with summary counts of associated claims and members.
    /// </summary>
    Task<Result<List<SecurityRoleDto>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the full detail of a single security role, including claims and members.
    /// </summary>
    Task<Result<SecurityRoleDetailDto>> GetByIdKeyAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Gets all available security claims in the system for the role-claim picker.
    /// </summary>
    Task<Result<List<SecurityClaimDto>>> GetAllClaimsAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new security role.
    /// </summary>
    Task<Result<SecurityRoleDto>> CreateAsync(CreateSecurityRoleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates the mutable fields of an existing security role. System roles cannot be renamed
    /// or deactivated via this method.
    /// </summary>
    Task<Result<SecurityRoleDto>> UpdateAsync(string idKey, UpdateSecurityRoleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a security role. Refuses to delete system roles.
    /// </summary>
    Task<Result> DeleteAsync(string idKey, CancellationToken ct = default);

    /// <summary>
    /// Assigns a claim to a role with an Allow or Deny decision.
    /// If the claim is already associated with the role, the decision is updated.
    /// </summary>
    Task<Result> AssignClaimAsync(string roleIdKey, AssignClaimRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes a claim from a role.
    /// </summary>
    Task<Result> RemoveClaimAsync(string roleIdKey, string claimIdKey, CancellationToken ct = default);

    /// <summary>
    /// Adds a person to a role, optionally with an expiration date.
    /// If the person is already a member, the expiration is updated.
    /// </summary>
    Task<Result> AddMemberAsync(string roleIdKey, AssignPersonRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes a person from a role.
    /// </summary>
    Task<Result> RemoveMemberAsync(string roleIdKey, string personIdKey, CancellationToken ct = default);
}
