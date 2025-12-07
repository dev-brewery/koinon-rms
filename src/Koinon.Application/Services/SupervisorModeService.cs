using System.Security.Cryptography;
using System.Text;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for supervisor mode operations in check-in kiosks.
/// Provides PIN-based authentication and session management.
/// </summary>
public class SupervisorModeService(
    IApplicationDbContext context,
    ILogger<SupervisorModeService> logger) : ISupervisorModeService
{
    private const int SessionDurationMinutes = 2; // Auto-logout after 2 minutes
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 15;

    // In-memory rate limiting (for MVP; use Redis in production)
    private static readonly Dictionary<string, RateLimitInfo> _rateLimitCache = new();
    private static readonly object _rateLimitLock = new();

    private class RateLimitInfo
    {
        public int FailedAttempts { get; set; }
        public DateTime LockedUntil { get; set; }
    }

    public async Task<SupervisorLoginResponse?> LoginAsync(
        SupervisorLoginRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        logger.LogInformation("Supervisor login attempt from IP: {IP}", ipAddress ?? "unknown");

        // Rate limiting check
        if (!string.IsNullOrEmpty(ipAddress) && IsRateLimited(ipAddress))
        {
            logger.LogWarning("Supervisor login blocked: Too many attempts from IP: {IP}", ipAddress);
            await LogAuditAsync(null, null, "LoginFailed", ipAddress, null, null, false, "Too many attempts", ct);
            await AddJitterAsync(ct);
            return null;
        }

        // Validate PIN format (4-6 digits)
        if (string.IsNullOrWhiteSpace(request.Pin) ||
            !request.Pin.All(char.IsDigit) ||
            request.Pin.Length < 4 ||
            request.Pin.Length > 6)
        {
            logger.LogWarning("Supervisor login failed: Invalid PIN format");
            RecordFailedAttempt(ipAddress);
            await LogAuditAsync(null, null, "LoginFailed", ipAddress, null, null, false, "Invalid PIN format", ct);
            await AddJitterAsync(ct);
            return null;
        }

        // Find person with supervisor PIN
        var person = await context.People
            .Where(p => p.SupervisorPinHash != null)
            .Include(p => p.RecordStatusValue)
            .ToListAsync(ct);

        Person? authenticatedSupervisor = null;

        // Check each person with a supervisor PIN
        // SECURITY: Iterate through ALL to prevent timing attacks
        foreach (var p in person)
        {
            if (await ValidatePinAsync(p, request.Pin, ct))
            {
                authenticatedSupervisor = p;
                // DO NOT break - continue checking all to prevent timing leaks
            }
        }

        if (authenticatedSupervisor == null)
        {
            logger.LogWarning("Supervisor login failed: Invalid PIN");
            RecordFailedAttempt(ipAddress);
            await LogAuditAsync(null, null, "LoginFailed", ipAddress, null, null, false, "Invalid PIN", ct);
            await AddJitterAsync(ct);
            return null;
        }

        // Check if person is active
        if (authenticatedSupervisor.RecordStatusValueId.HasValue)
        {
            var recordStatus = authenticatedSupervisor.RecordStatusValue?.Value;
            if (recordStatus != null && !recordStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Supervisor login failed: Person {PersonId} has inactive record status: {Status}",
                    authenticatedSupervisor.Id,
                    recordStatus);
                RecordFailedAttempt(ipAddress);
                await LogAuditAsync(authenticatedSupervisor.Id, null, "LoginFailed", ipAddress, null, null, false, "Inactive record status", ct);
                await AddJitterAsync(ct);
                return null;
            }
        }

        // Create supervisor session
        var sessionToken = GenerateSessionToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(SessionDurationMinutes);

        var session = new SupervisorSession
        {
            PersonId = authenticatedSupervisor.Id,
            Token = sessionToken,
            ExpiresAt = expiresAt,
            CreatedByIp = ipAddress,
            CreatedDateTime = DateTime.UtcNow
        };

        context.SupervisorSessions.Add(session);
        await context.SaveChangesAsync(ct);

        // Reset failed attempts on successful login
        ResetFailedAttempts(ipAddress);

        // Log successful login
        await LogAuditAsync(authenticatedSupervisor.Id, session.Id, "Login", ipAddress, null, null, true, null, ct);

        logger.LogInformation(
            "Supervisor login successful: PersonId={PersonId}, SessionId={SessionId}",
            authenticatedSupervisor.Id,
            session.Id);

        return new SupervisorLoginResponse
        {
            SessionToken = sessionToken,
            ExpiresAt = expiresAt,
            Supervisor = new SupervisorInfoDto
            {
                IdKey = IdKeyHelper.Encode(authenticatedSupervisor.Id),
                FullName = authenticatedSupervisor.FullName,
                FirstName = authenticatedSupervisor.FirstName,
                LastName = authenticatedSupervisor.LastName
            }
        };
    }

    public async Task<SupervisorInfoDto?> ValidateSessionAsync(
        string sessionToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var session = await context.SupervisorSessions
            .Include(s => s.Person)
            .FirstOrDefaultAsync(s => s.Token == sessionToken, ct);

        if (session == null || !session.IsActive)
        {
            return null;
        }

        return new SupervisorInfoDto
        {
            IdKey = IdKeyHelper.Encode(session.Person!.Id),
            FullName = session.Person.FullName,
            FirstName = session.Person.FirstName,
            LastName = session.Person.LastName
        };
    }

    public async Task<bool> LogoutAsync(string sessionToken, CancellationToken ct = default)
    {
        logger.LogInformation("Supervisor logout attempt");

        var session = await context.SupervisorSessions
            .FirstOrDefaultAsync(s => s.Token == sessionToken, ct);

        if (session == null)
        {
            logger.LogWarning("Supervisor logout failed: Session not found");
            return false;
        }

        if (!session.IsActive)
        {
            logger.LogWarning("Supervisor logout failed: Session already ended");
            return false;
        }

        // End the session
        session.EndedAt = DateTime.UtcNow;
        session.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        // Log logout
        await LogAuditAsync(session.PersonId, session.Id, "Logout", session.CreatedByIp, null, null, true, null, ct);

        logger.LogInformation("Supervisor logout successful: PersonId={PersonId}", session.PersonId);
        return true;
    }

    public async Task LogActionAsync(
        string sessionToken,
        string action,
        string? entityType = null,
        string? entityIdKey = null,
        CancellationToken ct = default)
    {
        var session = await context.SupervisorSessions
            .FirstOrDefaultAsync(s => s.Token == sessionToken, ct);

        if (session == null)
        {
            logger.LogWarning("Cannot log supervisor action: Invalid session token");
            return;
        }

        // Log to audit table
        await LogAuditAsync(
            session.PersonId,
            session.Id,
            action,
            session.CreatedByIp,
            entityType,
            entityIdKey,
            true,
            null,
            ct);

        logger.LogInformation(
            "Supervisor action: PersonId={PersonId}, Action={Action}, EntityType={EntityType}, EntityIdKey={EntityIdKey}",
            session.PersonId,
            action,
            entityType ?? "N/A",
            entityIdKey ?? "N/A");
    }

    /// <summary>
    /// Validates a PIN against a person's stored supervisor PIN hash.
    /// Uses Argon2id for secure PIN verification with constant-time comparison.
    /// </summary>
    private async Task<bool> ValidatePinAsync(Person person, string pin, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(person.SupervisorPinHash))
        {
            return false;
        }

        try
        {
            var combined = Convert.FromBase64String(person.SupervisorPinHash);
            if (combined.Length < 48) // 16 salt + 32 hash minimum
            {
                return false;
            }

            var salt = new byte[16];
            var storedHash = new byte[combined.Length - 16];
            Buffer.BlockCopy(combined, 0, salt, 0, 16);
            Buffer.BlockCopy(combined, 16, storedHash, 0, combined.Length - 16);

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(pin))
            {
                Salt = salt,
                DegreeOfParallelism = 8,
                Iterations = 4,
                MemorySize = 256 * 1024 // 256 MB per OWASP recommendation
            };

            var computedHash = await argon2.GetBytesAsync(32);
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PIN validation error for person {PersonId}", person.Id);
            return false;
        }
    }

    /// <summary>
    /// Generates a cryptographically random session token.
    /// </summary>
    private static string GenerateSessionToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Adds random delay to prevent timing attacks.
    /// </summary>
    private static async Task AddJitterAsync(CancellationToken ct)
    {
        await Task.Delay(Random.Shared.Next(100, 300), ct);
    }

    /// <summary>
    /// Checks if an IP address is currently rate limited.
    /// </summary>
    private static bool IsRateLimited(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return false;
        }

        lock (_rateLimitLock)
        {
            if (_rateLimitCache.TryGetValue(ipAddress, out var info))
            {
                if (info.LockedUntil > DateTime.UtcNow)
                {
                    return true;
                }

                // Lock expired, remove from cache and allow request
                _rateLimitCache.Remove(ipAddress);
                return false;
            }

            return false;
        }
    }

    /// <summary>
    /// Records a failed login attempt for rate limiting.
    /// </summary>
    private static void RecordFailedAttempt(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }

        lock (_rateLimitLock)
        {
            if (!_rateLimitCache.TryGetValue(ipAddress, out var info))
            {
                info = new RateLimitInfo();
                _rateLimitCache[ipAddress] = info;
            }

            info.FailedAttempts++;

            if (info.FailedAttempts >= MaxFailedAttempts)
            {
                info.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
            }
        }
    }

    /// <summary>
    /// Resets failed login attempts for an IP address.
    /// </summary>
    private static void ResetFailedAttempts(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }

        lock (_rateLimitLock)
        {
            _rateLimitCache.Remove(ipAddress);
        }
    }

    /// <summary>
    /// Logs a supervisor audit event to the database.
    /// </summary>
    private async Task LogAuditAsync(
        int? personId,
        int? supervisorSessionId,
        string actionType,
        string? ipAddress,
        string? entityType,
        string? entityIdKey,
        bool success,
        string? details,
        CancellationToken ct)
    {
        try
        {
            var auditLog = new SupervisorAuditLog
            {
                PersonId = personId,
                SupervisorSessionId = supervisorSessionId,
                ActionType = actionType,
                IpAddress = ipAddress,
                EntityType = entityType,
                EntityIdKey = entityIdKey,
                Success = success,
                Details = details,
                CreatedDateTime = DateTime.UtcNow
            };

            context.SupervisorAuditLogs.Add(auditLog);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Don't fail the operation if audit logging fails
            logger.LogError(ex, "Failed to write supervisor audit log");
        }
    }
}
