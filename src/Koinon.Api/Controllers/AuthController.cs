using FluentValidation;
using Koinon.Application.DTOs.Auth;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Authentication controller for login, token refresh, and logout operations.
/// Provides JWT-based authentication for the API.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<LoginRequest> loginValidator,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Token response with access token, refresh token, and user info</returns>
    /// <response code="200">Login successful, returns tokens and user info</response>
    /// <response code="400">Invalid request (validation errors)</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        // Validate request
        var validationResult = await loginValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Get client IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Attempt login
        var response = await authService.LoginAsync(request, ipAddress, ct);

        if (response == null)
        {
            logger.LogWarning("Login failed for email: {Email}", request.Email);

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Login successful for user: {Email}", request.Email);

        return Ok(response);
    }

    /// <summary>
    /// Generates a new access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New token response with rotated refresh token</returns>
    /// <response code="200">Token refresh successful</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Refresh token is required.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var response = await authService.RefreshTokenAsync(request.RefreshToken, ipAddress, ct);

        if (response == null)
        {
            logger.LogWarning("Refresh token failed");

            return Unauthorized(new ProblemDetails
            {
                Title = "Token refresh failed",
                Detail = "Invalid or expired refresh token.",
                Status = StatusCodes.Status401Unauthorized,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Token refresh successful");

        return Ok(response);
    }

    /// <summary>
    /// Revokes a refresh token, effectively logging out the user.
    /// </summary>
    /// <param name="request">Logout request with refresh token</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Logout successful</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Refresh token is required.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var success = await authService.LogoutAsync(request.RefreshToken, ipAddress, ct);

        if (!success)
        {
            logger.LogWarning("Logout failed: Token not found or already revoked");
        }
        else
        {
            logger.LogInformation("Logout successful");
        }

        // Return 204 regardless of whether token was found
        // (prevents information leakage about token validity)
        return NoContent();
    }
}

/// <summary>
/// Request DTO for token refresh and logout operations.
/// </summary>
public record RefreshTokenRequest(string RefreshToken);
