using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Common;

/// <summary>
/// Base class for all check-in services.
/// Enforces consistent authorization patterns where both person AND location access
/// is verified before any business logic executes.
///
/// PATTERN RULES:
/// 1. All public methods MUST call Authorize* methods before accessing data
/// 2. Never proceed if authorization throws
/// 3. Never use PersonAlias as implicit authorization (always call CanAccessPerson)
/// 4. All exception types must be consistent (throw UnauthorizedAccessException)
///
/// BENEFITS:
/// - Impossible to forget authorization checks
/// - Consistent exception handling
/// - No timing windows where auth can be bypassed
/// - Single source of truth for authorization policy
///
/// See: ARCHITECTURAL_REVIEW_PHASE2.2.md#authorization-as-first-class
/// </summary>
public abstract class AuthorizedCheckinService(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger logger)
{
    protected IApplicationDbContext Context => context;
    protected IUserContext UserContext => userContext;
    protected ILogger Logger => logger;

    /// <summary>
    /// Verifies that:
    /// 1. A user is authenticated
    /// 2. The user can access the specified person
    ///
    /// Throws immediately if either check fails. No return value.
    /// Use this at the START of any method that operates on a person.
    ///
    /// EXAMPLE:
    ///   public async Task<AttendanceDto> CheckInAsync(int personId, ...)
    ///   {
    ///       AuthorizePersonAccess(personId, nameof(CheckInAsync));
    ///       // Safe to access personId from here on
    ///   }
    ///
    /// ANTI-PATTERN (DON'T DO THIS):
    ///   if (!userContext.CanAccessPerson(personId)) {
    ///       return GenericError();  // Inconsistent response
    ///   }
    /// </summary>
    /// <param name="personId">ID of person being accessed</param>
    /// <param name="operationName">Name of operation (for logging)</param>
    /// <exception cref="UnauthorizedAccessException">If not authenticated or no access</exception>
    protected void AuthorizePersonAccess(int personId, string operationName)
    {
        if (!userContext.IsAuthenticated)
        {
            logger.LogWarning("Unauthenticated access attempt: {Operation}", operationName);
            throw new UnauthorizedAccessException("Authentication required");
        }

        if (!userContext.CanAccessPerson(personId))
        {
            logger.LogWarning(
                "Authorization denied: User {UserId} denied access for {Operation} on person {PersonId}",
                userContext.CurrentPersonId, operationName, personId);
            throw new UnauthorizedAccessException("Not authorized for this operation");
        }
    }

    /// <summary>
    /// Verifies that:
    /// 1. A user is authenticated
    /// 2. The user can access the specified location
    ///
    /// Throws immediately if either check fails. No return value.
    /// Use this whenever a method operates on a location (check-in area, room, etc).
    ///
    /// EXAMPLE:
    ///   public async Task<ConfigurationDto> GetLocationConfigAsync(int locationId, ...)
    ///   {
    ///       AuthorizeLocationAccess(locationId, nameof(GetLocationConfigAsync));
    ///       // Safe to access locationId from here on
    ///   }
    /// </summary>
    /// <param name="locationId">ID of location being accessed</param>
    /// <param name="operationName">Name of operation (for logging)</param>
    /// <exception cref="UnauthorizedAccessException">If not authenticated or no access</exception>
    protected void AuthorizeLocationAccess(int locationId, string operationName)
    {
        if (!userContext.IsAuthenticated)
        {
            logger.LogWarning("Unauthenticated access attempt: {Operation}", operationName);
            throw new UnauthorizedAccessException("Authentication required");
        }

        if (!userContext.CanAccessLocation(locationId))
        {
            logger.LogWarning(
                "Authorization denied: User {UserId} denied access for {Operation} on location {LocationId}",
                userContext.CurrentPersonId, operationName, locationId);
            throw new UnauthorizedAccessException("Not authorized for this operation");
        }
    }

    /// <summary>
    /// Verifies that a user can access BOTH a person AND a location.
    /// This is the most common pattern for check-in operations.
    ///
    /// Throws immediately if ANY check fails.
    ///
    /// EXAMPLE (CORRECT USAGE):
    ///   public async Task<CheckinResultDto> CheckInAsync(
    ///       int personId, int locationId, ...)
    ///   {
    ///       AuthorizeCheckinOperation(personId, locationId, nameof(CheckInAsync));
    ///       // From here on, user is guaranteed access to both
    ///       var person = await context.People.FindAsync(personId);
    ///       var location = await context.Groups.FindAsync(locationId);
    ///       // ...create attendance...
    ///   }
    ///
    /// WHY NOT AuthorizePersonAccess + AuthorizeLocationAccess SEPARATELY:
    ///   1. Single call is cleaner and more expressive
    ///   2. Logs as single operation, not two
    ///   3. Easier to add compound logic later (e.g., location campus matches person campus)
    /// </summary>
    /// <param name="personId">ID of person being checked in</param>
    /// <param name="locationId">ID of location for check-in</param>
    /// <param name="operationName">Name of operation (for logging)</param>
    /// <exception cref="UnauthorizedAccessException">If user lacks access to person OR location</exception>
    protected void AuthorizeCheckinOperation(int personId, int locationId, string operationName)
    {
        if (!userContext.IsAuthenticated)
        {
            logger.LogWarning("Unauthenticated access attempt: {Operation}", operationName);
            throw new UnauthorizedAccessException("Authentication required");
        }

        if (!userContext.CanAccessPerson(personId))
        {
            logger.LogWarning(
                "Authorization denied: User {UserId} denied person access for {Operation}",
                userContext.CurrentPersonId, operationName);
            throw new UnauthorizedAccessException("Not authorized for this operation");
        }

        if (!userContext.CanAccessLocation(locationId))
        {
            logger.LogWarning(
                "Authorization denied: User {UserId} denied location access for {Operation}",
                userContext.CurrentPersonId, operationName);
            throw new UnauthorizedAccessException("Not authorized for this operation");
        }
    }

    /// <summary>
    /// Verifies authenticated but no specific resource access required.
    /// Use for: reading configuration, listing available options, etc.
    ///
    /// EXAMPLE:
    ///   public async Task<List<CampusDto>> GetAvailableCampusesAsync()
    ///   {
    ///       AuthorizeAuthentication("GetAvailableCampusesAsync");
    ///       // User authenticated but doesn't need specific resource access
    ///   }
    /// </summary>
    protected void AuthorizeAuthentication(string operationName)
    {
        if (!userContext.IsAuthenticated)
        {
            logger.LogWarning("Unauthenticated access attempt: {Operation}", operationName);
            throw new UnauthorizedAccessException("Authentication required");
        }
    }

    /// <summary>
    /// Generic "operation not authorized" response for public methods.
    /// Use this in catch blocks to return a user-friendly error without revealing why.
    ///
    /// EXAMPLE:
    ///   try {
    ///       AuthorizeCheckinOperation(personId, locationId, "CheckInAsync");
    ///       // ... do work ...
    ///   } catch (UnauthorizedAccessException ex) {
    ///       logger.LogWarning(ex, "CheckIn denied");
    ///       return new CheckinResultDto(
    ///           Success: false,
    ///           ErrorMessage: GenericAuthorizationDeniedMessage());
    ///   }
    /// </summary>
    protected static string GenericAuthorizationDeniedMessage()
        => "Not authorized for this operation";

    /// <summary>
    /// Logs an authorization failure in a way that doesn't reveal WHICH check failed.
    /// This prevents information disclosure through error messages.
    ///
    /// EXAMPLE:
    ///   try {
    ///       AuthorizeCheckinOperation(personId, locationId, "CheckIn");
    ///   } catch (UnauthorizedAccessException ex) {
    ///       LogAuthorizationFailure(personId, locationId, "CheckIn");
    ///       // Don't log the exception message (it might say "can't access person"
    ///       // which tells attacker the person exists)
    ///   }
    /// </summary>
    protected void LogAuthorizationFailure(int personId, int locationId, string operationName)
    {
        // Generic message without revealing which check failed
        logger.LogWarning(
            "Authorization failure for operation {Operation}: " +
            "User {UserId} was denied access to person {PersonId} and/or location {LocationId}",
            operationName, userContext.CurrentPersonId, personId, locationId);
    }
}
