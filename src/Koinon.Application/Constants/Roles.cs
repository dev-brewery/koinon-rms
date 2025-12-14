namespace Koinon.Application.Constants;

/// <summary>
/// Defines standard role names used throughout the application.
/// These roles are checked via IUserContext.IsInRole() for authorization decisions.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Administrator role with full system access.
    /// Can update any person's data including photos.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Staff role with access to manage person data.
    /// Can update any person's data including photos.
    /// </summary>
    public const string Staff = "Staff";

    /// <summary>
    /// Check-in worker role with location-specific access.
    /// Can perform check-in operations at assigned locations.
    /// </summary>
    public const string CheckInWorker = "CheckInWorker";
}
