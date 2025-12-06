namespace Koinon.Application.DTOs.Auth;

/// <summary>
/// Login request DTO containing user credentials.
/// </summary>
public record LoginRequest(string Email, string Password);
