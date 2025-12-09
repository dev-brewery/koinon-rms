using Koinon.Application.Configuration;
using Koinon.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Koinon.Application.Services;

/// <summary>
/// Service for rate limiting pickup verification attempts to prevent brute-force attacks on security codes.
/// Tracks failed verification attempts by {AttendanceIdKey}:{ClientIP} and enforces a configurable maximum
/// number of attempts per time window to prevent attackers from trying all 1,000,000 possible 6-digit security codes.
/// Uses IMemoryCache with automatic TTL expiration to prevent memory leaks.
/// </summary>
public class PickupRateLimitService : IPickupRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;
    private readonly ILogger<PickupRateLimitService> _logger;

    public PickupRateLimitService(
        IMemoryCache cache,
        IOptions<RateLimitOptions> options,
        ILogger<PickupRateLimitService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the rate limit has been exceeded for a given attendance record and client IP.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    /// <returns>True if rate limited (max attempts exceeded), false otherwise</returns>
    public bool IsRateLimited(string attendanceIdKey, string clientIp)
    {
        var key = GetCacheKey(attendanceIdKey, clientIp);

        if (_cache.TryGetValue<int>(key, out var failedAttempts))
        {
            if (failedAttempts >= _options.MaxAttempts)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for pickup verification: AttendanceIdKey={AttendanceIdKey}, ClientIP={ClientIp}, Attempts={Attempts}",
                    attendanceIdKey, clientIp, failedAttempts);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Records a failed verification attempt.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    public void RecordFailedAttempt(string attendanceIdKey, string clientIp)
    {
        var key = GetCacheKey(attendanceIdKey, clientIp);
        var window = TimeSpan.FromMinutes(_options.WindowMinutes);

        var currentAttempts = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            return 0;
        });

        var newAttempts = currentAttempts + 1;
        _cache.Set(key, newAttempts, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = window
        });

        _logger.LogWarning(
            "Failed pickup verification attempt recorded: AttendanceIdKey={AttendanceIdKey}, ClientIp={ClientIp}, AttemptCount={AttemptCount}",
            attendanceIdKey, clientIp, newAttempts);
    }

    /// <summary>
    /// Resets the failed attempt counter for a given attendance record and client IP.
    /// Called when a verification succeeds.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    public void ResetAttempts(string attendanceIdKey, string clientIp)
    {
        var key = GetCacheKey(attendanceIdKey, clientIp);
        _cache.Remove(key);

        _logger.LogInformation(
            "Pickup rate limit reset: AttendanceIdKey={AttendanceIdKey}, ClientIp={ClientIp}",
            attendanceIdKey, clientIp);
    }

    /// <summary>
    /// Gets the remaining time until the rate limit window expires.
    /// Note: With IMemoryCache, we cannot get exact expiration time, so we return
    /// the full window as a conservative estimate.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    /// <returns>TimeSpan until window expires, or null if not rate limited</returns>
    public TimeSpan? GetRetryAfter(string attendanceIdKey, string clientIp)
    {
        var key = GetCacheKey(attendanceIdKey, clientIp);

        if (_cache.TryGetValue<int>(key, out var failedAttempts) && failedAttempts >= _options.MaxAttempts)
        {
            // Return the full window as conservative estimate
            return TimeSpan.FromMinutes(_options.WindowMinutes);
        }

        return null;
    }

    /// <summary>
    /// Generates the cache key for tracking rate limit attempts.
    /// </summary>
    /// <param name="attendanceIdKey">The attendance record IdKey</param>
    /// <param name="clientIp">The client IP address</param>
    /// <returns>Cache key in format "pickup-ratelimit:{attendanceIdKey}:{clientIp}"</returns>
    private string GetCacheKey(string attendanceIdKey, string clientIp)
    {
        return $"pickup-ratelimit:{attendanceIdKey}:{clientIp}";
    }
}
