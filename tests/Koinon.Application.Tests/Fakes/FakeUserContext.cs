using Koinon.Application.Interfaces;

namespace Koinon.Application.Tests.Fakes;

/// <summary>
/// Fake implementation of IUserContext for testing.
/// By default, allows all operations. Can be configured to test authorization failures.
/// </summary>
public class FakeUserContext : IUserContext
{
    /// <summary>
    /// Gets or sets the ID of the currently authenticated person.
    /// Default is 1 (simulating an authenticated user).
    /// </summary>
    public int? CurrentPersonId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the ID of the current organization.
    /// Default is 1.
    /// </summary>
    public int? CurrentOrganizationId { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether the user is authenticated.
    /// Default is true.
    /// </summary>
    public bool IsAuthenticated { get; set; } = true;

    /// <summary>
    /// Gets or sets the IP address of the client making the request.
    /// Default is null (simulating background job or test context).
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent header of the client making the request.
    /// Default is null (simulating background job or test context).
    /// </summary>
    public string? ClientUserAgent { get; set; }

    /// <summary>
    /// Checks if the current user is in the specified role.
    /// Default implementation returns true for all roles.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns>True (default).</returns>
    public bool IsInRole(string role) => true;

    /// <summary>
    /// Determines if the current user can access data for the specified person.
    /// Default implementation returns true for all persons.
    /// Override or set to false for specific test scenarios.
    /// </summary>
    /// <param name="personId">The ID of the person to check access for.</param>
    /// <returns>True (default).</returns>
    public bool CanAccessPerson(int personId) => true;

    /// <summary>
    /// Determines if the current user can access the specified location.
    /// Default implementation returns true for all locations.
    /// Override or set to false for specific test scenarios.
    /// </summary>
    /// <param name="locationId">The ID of the location to check access for.</param>
    /// <returns>True (default).</returns>
    public bool CanAccessLocation(int locationId) => true;

    /// <summary>
    /// Determines if the current user can access the specified family.
    /// Default implementation returns true for all families.
    /// Override or set to false for specific test scenarios.
    /// </summary>
    /// <param name="familyId">The ID of the family to check access for.</param>
    /// <returns>True (default).</returns>
    public bool CanAccessFamily(int familyId) => true;
}
