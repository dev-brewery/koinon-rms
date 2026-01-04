using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for managing user settings and preferences.
/// Provides endpoints for display preferences, password changes, session management, and two-factor authentication.
/// All endpoints operate on the currently authenticated user's settings.
/// </summary>
[ApiController]
[Route("api/v1/my-settings")]
[Authorize]
public class UserSettingsController(
    IUserSettingsService userSettingsService,
    IUserContext userContext,
    ILogger<UserSettingsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets the current user's display preferences (theme, date format, timezone).
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User preferences</returns>
    /// <response code="200">Returns user preferences</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to get preferences");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to access user preferences",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.GetPreferencesAsync(userContext.CurrentPersonId.Value, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to get preferences: PersonId={PersonId}, Code={Code}, Message={Message}",
                userContext.CurrentPersonId, result.Error!.Code, result.Error.Message);

            return UnprocessableEntity(new ProblemDetails
            {
                Title = result.Error.Code,
                Detail = result.Error.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("User preferences retrieved: PersonId={PersonId}", userContext.CurrentPersonId);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Updates the current user's display preferences.
    /// </summary>
    /// <param name="request">Updated preference values</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated preferences</returns>
    /// <response code="200">Preferences updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">User not authenticated</response>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateUserPreferenceRequest request,
        CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to update preferences");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to update user preferences",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.UpdatePreferencesAsync(
            userContext.CurrentPersonId.Value,
            request,
            ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update preferences: PersonId={PersonId}, Code={Code}, Message={Message}",
                userContext.CurrentPersonId, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Message,
                    Detail = result.Error.Details != null
                        ? string.Join("; ", result.Error.Details.SelectMany(kvp => kvp.Value))
                        : null,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details ?? new Dictionary<string, string[]>() }
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogInformation(
            "User preferences updated successfully: PersonId={PersonId}",
            userContext.CurrentPersonId);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Changes the current user's password.
    /// Requires the current password for verification.
    /// </summary>
    /// <param name="request">Password change details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Password changed successfully</response>
    /// <response code="400">Validation failed or current password incorrect</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to change password");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to change your password",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.ChangePasswordAsync(
            userContext.CurrentPersonId.Value,
            request,
            ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to change password: PersonId={PersonId}, Code={Code}",
                userContext.CurrentPersonId, result.Error!.Code);

            var problemDetails = new ProblemDetails
            {
                Title = result.Error.Code,
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            };

            if (result.Error.Details != null)
            {
                problemDetails.Extensions["errors"] = result.Error.Details;
            }

            return BadRequest(problemDetails);
        }

        logger.LogInformation(
            "Password changed successfully: PersonId={PersonId}",
            userContext.CurrentPersonId);

        return NoContent();
    }

    /// <summary>
    /// Lists all active sessions for the current user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    /// <response code="200">Returns list of sessions</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions(CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to get sessions");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to view your sessions",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.GetSessionsAsync(userContext.CurrentPersonId.Value, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to get sessions: PersonId={PersonId}, Code={Code}, Message={Message}",
                userContext.CurrentPersonId, result.Error!.Code, result.Error.Message);

            return UnprocessableEntity(new ProblemDetails
            {
                Title = result.Error.Code,
                Detail = result.Error.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug(
            "User sessions retrieved: PersonId={PersonId}, SessionCount={SessionCount}",
            userContext.CurrentPersonId, result.Value!.Count);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Revokes a specific session by its IdKey.
    /// Cannot revoke the current session.
    /// </summary>
    /// <param name="idKey">The session's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Session revoked successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Session not found</response>
    /// <response code="422">Business rule violation (e.g., cannot revoke current session)</response>
    [HttpDelete("sessions/{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RevokeSession(string idKey, CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to revoke session");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to revoke sessions",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.RevokeSessionAsync(
            userContext.CurrentPersonId.Value,
            idKey,
            ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to revoke session: PersonId={PersonId}, SessionIdKey={SessionIdKey}, Code={Code}, Message={Message}",
                userContext.CurrentPersonId, idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Session not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogInformation(
            "Session revoked successfully: PersonId={PersonId}, SessionIdKey={SessionIdKey}",
            userContext.CurrentPersonId, idKey);

        return NoContent();
    }

    /// <summary>
    /// Gets the two-factor authentication status for the current user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>2FA status (enabled/disabled)</returns>
    /// <response code="200">Returns 2FA status</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("two-factor")]
    [ProducesResponseType(typeof(TwoFactorStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTwoFactorStatus(CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to get 2FA status");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to view 2FA status",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.GetTwoFactorStatusAsync(userContext.CurrentPersonId.Value, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to get 2FA status: PersonId={PersonId}, Code={Code}, Message={Message}",
                userContext.CurrentPersonId, result.Error!.Code, result.Error.Message);

            return UnprocessableEntity(new ProblemDetails
            {
                Title = result.Error.Code,
                Detail = result.Error.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("2FA status retrieved: PersonId={PersonId}", userContext.CurrentPersonId);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Initiates two-factor authentication setup for the current user.
    /// Returns the secret key, QR code URI, and recovery codes.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>2FA setup details</returns>
    /// <response code="200">Returns 2FA setup information</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="422">2FA already enabled</response>
    [HttpPost("two-factor/setup")]
    [ProducesResponseType(typeof(TwoFactorSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to setup 2FA");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to setup 2FA",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.SetupTwoFactorAsync(userContext.CurrentPersonId.Value, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to setup 2FA: PersonId={PersonId}, Code={Code}, Message={Message}",
                userContext.CurrentPersonId, result.Error!.Code, result.Error.Message);

            return UnprocessableEntity(new ProblemDetails
            {
                Title = result.Error.Code,
                Detail = result.Error.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "2FA setup initiated: PersonId={PersonId}",
            userContext.CurrentPersonId);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Verifies a two-factor authentication code and enables 2FA for the current user.
    /// </summary>
    /// <param name="request">Verification code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">2FA verified and enabled successfully</response>
    /// <response code="400">Invalid verification code</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("two-factor/verify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyTwoFactor(
        [FromBody] TwoFactorVerifyRequest request,
        CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to verify 2FA");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to verify 2FA",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.VerifyTwoFactorAsync(
            userContext.CurrentPersonId.Value,
            request,
            ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to verify 2FA: PersonId={PersonId}, Code={Code}",
                userContext.CurrentPersonId, result.Error!.Code);

            var problemDetails = new ProblemDetails
            {
                Title = result.Error.Code,
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            };

            if (result.Error.Details != null)
            {
                problemDetails.Extensions["errors"] = result.Error.Details;
            }

            return BadRequest(problemDetails);
        }

        logger.LogInformation(
            "2FA verified and enabled: PersonId={PersonId}",
            userContext.CurrentPersonId);

        return NoContent();
    }

    /// <summary>
    /// Disables two-factor authentication for the current user.
    /// Requires verification code for security.
    /// </summary>
    /// <param name="request">Verification code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">2FA disabled successfully</response>
    /// <response code="400">Invalid verification code</response>
    /// <response code="401">User not authenticated</response>
    [HttpDelete("two-factor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DisableTwoFactor(
        [FromBody] TwoFactorVerifyRequest request,
        CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.CurrentPersonId == null)
        {
            logger.LogWarning("Unauthenticated access attempt to disable 2FA");

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication required",
                Detail = "You must be authenticated to disable 2FA",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await userSettingsService.DisableTwoFactorAsync(
            userContext.CurrentPersonId.Value,
            request,
            ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to disable 2FA: PersonId={PersonId}, Code={Code}",
                userContext.CurrentPersonId, result.Error!.Code);

            var problemDetails = new ProblemDetails
            {
                Title = result.Error.Code,
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            };

            if (result.Error.Details != null)
            {
                problemDetails.Extensions["errors"] = result.Error.Details;
            }

            return BadRequest(problemDetails);
        }

        logger.LogInformation(
            "2FA disabled: PersonId={PersonId}",
            userContext.CurrentPersonId);

        return NoContent();
    }
}
