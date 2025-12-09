namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for rate limiting pickup verification attempts.
/// Prevents brute-force attacks on 6-digit security codes by limiting failed attempts
/// to 5 per 15 minutes per attendance record and client IP combination.
/// </summary>
public interface IPickupRateLimitService
{
    /// <summary>
    /// Checks if the rate limit has been exceeded for a given attendance record and client IP.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    /// <returns>True if rate limited (max attempts exceeded), false otherwise</returns>
    bool IsRateLimited(string attendanceIdKey, string clientIp);

    /// <summary>
    /// Records a failed verification attempt.
    /// Increments the counter for the given attendance record and client IP.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    void RecordFailedAttempt(string attendanceIdKey, string clientIp);

    /// <summary>
    /// Resets the failed attempt counter for a given attendance record and client IP.
    /// Called when a verification succeeds.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    void ResetAttempts(string attendanceIdKey, string clientIp);

    /// <summary>
    /// Gets the remaining time until the rate limit window expires.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    /// <returns>TimeSpan until window expires, or null if not rate limited</returns>
    TimeSpan? GetRetryAfter(string attendanceIdKey, string clientIp);
}
