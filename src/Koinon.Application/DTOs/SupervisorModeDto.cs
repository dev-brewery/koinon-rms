namespace Koinon.Application.DTOs;

/// <summary>
/// Request to authenticate a supervisor using a PIN.
/// </summary>
public record SupervisorLoginRequest
{
    /// <summary>
    /// 4-6 digit PIN code for supervisor authentication.
    /// </summary>
    public required string Pin { get; init; }
}

/// <summary>
/// Response after successful supervisor authentication.
/// </summary>
public record SupervisorLoginResponse
{
    /// <summary>
    /// Session token for supervisor mode (time-limited).
    /// </summary>
    public required string SessionToken { get; init; }

    /// <summary>
    /// When the supervisor session expires (UTC).
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Supervisor information.
    /// </summary>
    public required SupervisorInfoDto Supervisor { get; init; }
}

/// <summary>
/// Information about the authenticated supervisor.
/// </summary>
public record SupervisorInfoDto
{
    /// <summary>
    /// IdKey of the supervisor person.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Full name of the supervisor.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// First name of the supervisor.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Last name of the supervisor.
    /// </summary>
    public required string LastName { get; init; }
}

/// <summary>
/// Request to reprint a label in supervisor mode.
/// </summary>
public record SupervisorReprintRequest
{
    /// <summary>
    /// Supervisor session token.
    /// </summary>
    public required string SessionToken { get; init; }

    /// <summary>
    /// IdKey of the attendance record to reprint.
    /// </summary>
    public required string AttendanceIdKey { get; init; }
}
