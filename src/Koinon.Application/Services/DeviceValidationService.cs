using System.Security.Cryptography;
using System.Text.Json;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for validating kiosk/device tokens with Redis caching.
/// Implements security checks for kiosk authentication.
/// </summary>
public class DeviceValidationService : IDeviceValidationService
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache? _cache;
    private readonly ILogger<DeviceValidationService> _logger;

    private const string CacheKeyPrefix = "kiosk:token:";
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

    public DeviceValidationService(
        IApplicationDbContext context,
        IDistributedCache? cache,
        ILogger<DeviceValidationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Validates a kiosk token and returns the device ID if valid.
    /// Uses Redis caching for performance.
    /// </summary>
    public async Task<int?> ValidateKioskTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        // Try cache first (if available)
        if (_cache != null)
        {
            var cacheKey = $"{CacheKeyPrefix}{token}";
            var cachedValue = await _cache.GetStringAsync(cacheKey, ct);

            if (!string.IsNullOrEmpty(cachedValue))
            {
                var cachedData = JsonSerializer.Deserialize<CachedTokenValidation>(cachedValue);
                if (cachedData != null)
                {
                    _logger.LogDebug(
                        "Kiosk token validation cache hit: DeviceId={DeviceId}",
                        cachedData.DeviceId);
                    return cachedData.DeviceId;
                }
            }
        }

        // Cache miss or no cache - query database
        var device = await _context.Devices
            .AsNoTracking()
            .Where(d => d.KioskToken == token)
            .Where(d => d.IsActive)
            .Where(d => d.KioskTokenExpiresAt == null || d.KioskTokenExpiresAt > DateTime.UtcNow)
            .Select(d => new { d.Id, d.Name })
            .FirstOrDefaultAsync(ct);

        if (device == null)
        {
            _logger.LogWarning(
                "Invalid kiosk token rejected: Token={TokenPrefix}...",
                token.Length > 8 ? token.Substring(0, 8) : token);
            return null;
        }

        _logger.LogInformation(
            "Kiosk token validated: DeviceId={DeviceId}, DeviceName={DeviceName}",
            device.Id, device.Name);

        // Cache the result (if cache available)
        if (_cache != null)
        {
            var cacheKey = $"{CacheKeyPrefix}{token}";
            var cacheData = new CachedTokenValidation
            {
                DeviceId = device.Id,
                DeviceName = device.Name,
                ValidatedAt = DateTime.UtcNow
            };

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(cacheData),
                cacheOptions,
                ct);

            _logger.LogDebug(
                "Kiosk token validation cached: DeviceId={DeviceId}, Duration={Duration}",
                device.Id, _cacheDuration);
        }

        return device.Id;
    }

    /// <summary>
    /// Revokes a kiosk token by clearing the token and invalidating cache.
    /// </summary>
    public async Task<bool> RevokeKioskTokenAsync(string deviceIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(deviceIdKey, out var deviceId))
        {
            _logger.LogWarning("Invalid IdKey format for token revocation: {IdKey}", deviceIdKey);
            return false;
        }

        var device = await _context.Devices
            .Where(d => d.Id == deviceId)
            .FirstOrDefaultAsync(ct);

        if (device == null)
        {
            _logger.LogWarning("Device not found for token revocation: IdKey={IdKey}", deviceIdKey);
            return false;
        }

        var oldToken = device.KioskToken;

        // Clear the token and expiration
        device.KioskToken = null;
        device.KioskTokenExpiresAt = null;
        device.ModifiedDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Invalidate cache (if available and token existed)
        if (_cache != null && !string.IsNullOrEmpty(oldToken))
        {
            var cacheKey = $"{CacheKeyPrefix}{oldToken}";
            await _cache.RemoveAsync(cacheKey, ct);

            _logger.LogInformation(
                "Kiosk token revoked and cache invalidated: DeviceId={DeviceId}, DeviceName={DeviceName}",
                device.Id, device.Name);
        }
        else
        {
            _logger.LogInformation(
                "Kiosk token revoked: DeviceId={DeviceId}, DeviceName={DeviceName}",
                device.Id, device.Name);
        }

        return true;
    }

    /// <summary>
    /// Generates a new cryptographically secure kiosk token for a device.
    /// </summary>
    public async Task<string> GenerateKioskTokenAsync(
        string deviceIdKey,
        DateTime? expiresAt = null,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(deviceIdKey, out var deviceId))
        {
            throw new ArgumentException("Invalid IdKey format", nameof(deviceIdKey));
        }

        var device = await _context.Devices
            .Where(d => d.Id == deviceId)
            .FirstOrDefaultAsync(ct);

        if (device == null)
        {
            throw new InvalidOperationException($"Device not found: {deviceIdKey}");
        }

        // Revoke old token if it exists (to invalidate cache)
        if (!string.IsNullOrEmpty(device.KioskToken) && _cache != null)
        {
            var oldCacheKey = $"{CacheKeyPrefix}{device.KioskToken}";
            await _cache.RemoveAsync(oldCacheKey, ct);
        }

        // Generate a new secure token (64 bytes = 128 hex characters)
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        // Update device
        device.KioskToken = token;
        device.KioskTokenExpiresAt = expiresAt;
        device.ModifiedDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "New kiosk token generated: DeviceId={DeviceId}, DeviceName={DeviceName}, ExpiresAt={ExpiresAt}",
            device.Id, device.Name, expiresAt);

        return token;
    }

    /// <summary>
    /// Internal class for caching token validation results.
    /// </summary>
    private class CachedTokenValidation
    {
        public required int DeviceId { get; init; }
        public required string DeviceName { get; init; }
        public required DateTime ValidatedAt { get; init; }
    }
}
