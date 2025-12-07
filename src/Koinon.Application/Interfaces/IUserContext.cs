namespace Koinon.Application.Interfaces;

/// <summary>
/// Provides access to the current user's identity and authorization context.
/// Used to enforce authorization checks across application services.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the ID of the currently authenticated person.
    /// Null if no user is authenticated.
    /// </summary>
    int? CurrentPersonId { get; }

    /// <summary>
    /// Gets the ID of the current organization (for multi-tenant support).
    /// Null if not applicable.
    /// </summary>
    int? CurrentOrganizationId { get; }

    /// <summary>
    /// Indicates whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns>True if the user is in the role, false otherwise.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Determines if the current user can access data for the specified person.
    /// This checks if the user is the person themselves, a family member, or has staff permissions.
    /// </summary>
    /// <param name="personId">The ID of the person to check access for.</param>
    /// <returns>True if access is allowed, false otherwise.</returns>
    bool CanAccessPerson(int personId);

    /// <summary>
    /// Determines if the current user can access the specified location (group).
    /// This checks if the user has permissions for check-in at this location.
    /// </summary>
    /// <param name="locationId">The ID of the location to check access for.</param>
    /// <returns>True if access is allowed, false otherwise.</returns>
    bool CanAccessLocation(int locationId);

    /// <summary>
    /// Determines if the current user can access data for the specified family.
    /// This checks if the user is a member of the family or has staff permissions.
    /// </summary>
    /// <param name="familyId">The ID of the family to check access for.</param>
    /// <returns>True if access is allowed, false otherwise.</returns>
    bool CanAccessFamily(int familyId);
}
