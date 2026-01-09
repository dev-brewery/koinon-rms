using System.Security.Cryptography;
using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.Constants;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing user settings including preferences, sessions, password, and two-factor authentication.
/// </summary>
public class UserSettingsService(
    IApplicationDbContext context,
    IAuthService authService,
    IValidator<UpdateUserPreferenceRequest> updatePreferenceValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator,
    IValidator<TwoFactorVerifyRequest> twoFactorVerifyValidator,
    ILogger<UserSettingsService> logger) : IUserSettingsService
{
    public async Task<Result<UserPreferenceDto>> GetPreferencesAsync(int personId, CancellationToken ct = default)
    {
        logger.LogInformation("Getting preferences for PersonId={PersonId}", personId);

        var preference = await context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, ct);

        if (preference == null)
        {
            // Create default preferences if none exist
            preference = new UserPreference
            {
                PersonId = personId,
                Theme = Theme.System,
                DateFormat = "MM/dd/yyyy",
                TimeZone = "America/New_York"
            };

            context.UserPreferences.Add(preference);
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Created default preferences for PersonId={PersonId}", personId);
        }

        var dto = new UserPreferenceDto
        {
            IdKey = preference.IdKey,
            Theme = preference.Theme,
            DateFormat = preference.DateFormat,
            TimeZone = preference.TimeZone,
            CreatedDateTime = preference.CreatedDateTime,
            ModifiedDateTime = preference.ModifiedDateTime
        };

        return Result<UserPreferenceDto>.Success(dto);
    }

    public async Task<Result<UserPreferenceDto>> UpdatePreferencesAsync(
        int personId,
        UpdateUserPreferenceRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await updatePreferenceValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<UserPreferenceDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        logger.LogInformation("Updating preferences for PersonId={PersonId}", personId);

        var preference = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.PersonId == personId, ct);

        if (preference == null)
        {
            // Create new preference if it doesn't exist
            preference = new UserPreference
            {
                PersonId = personId,
                Theme = request.Theme,
                DateFormat = request.DateFormat,
                TimeZone = request.TimeZone
            };

            context.UserPreferences.Add(preference);
        }
        else
        {
            // Update existing preference
            preference.Theme = request.Theme;
            preference.DateFormat = request.DateFormat;
            preference.TimeZone = request.TimeZone;
            preference.ModifiedDateTime = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated preferences for PersonId={PersonId}", personId);

        var dto = new UserPreferenceDto
        {
            IdKey = preference.IdKey,
            Theme = preference.Theme,
            DateFormat = preference.DateFormat,
            TimeZone = preference.TimeZone,
            CreatedDateTime = preference.CreatedDateTime,
            ModifiedDateTime = preference.ModifiedDateTime
        };

        return Result<UserPreferenceDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<UserSessionDto>>> GetSessionsAsync(int personId, CancellationToken ct = default)
    {
        logger.LogInformation("Getting sessions for PersonId={PersonId}", personId);

        var sessions = await context.UserSessions
            .AsNoTracking()
            .Where(s => s.PersonId == personId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(ct);

        // Get current session ID from refresh token if available
        var currentRefreshTokenId = await GetCurrentRefreshTokenIdAsync(personId, ct);

        var dtos = sessions.Select(s => new UserSessionDto
        {
            IdKey = s.IdKey,
            DeviceInfo = s.DeviceInfo,
            IpAddress = s.IpAddress,
            Location = s.Location,
            LastActivityAt = s.LastActivityAt,
            IsActive = s.IsActive,
            IsCurrentSession = s.RefreshTokenId == currentRefreshTokenId,
            CreatedDateTime = s.CreatedDateTime
        }).ToList();

        return Result<IReadOnlyList<UserSessionDto>>.Success(dtos);
    }

    public async Task<Result> RevokeSessionAsync(int personId, string sessionIdKey, CancellationToken ct = default)
    {
        logger.LogInformation("Revoking session {SessionIdKey} for PersonId={PersonId}", sessionIdKey, personId);

        var sessionId = IdKeyHelper.Decode(sessionIdKey);
        if (sessionId == 0)
        {
            return Result.Failure(Error.Validation("Invalid session IdKey"));
        }

        var session = await context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.PersonId == personId, ct);

        if (session == null)
        {
            return Result.Failure(Error.NotFound("UserSession", sessionIdKey));
        }

        // Mark session as inactive
        session.IsActive = false;
        session.ModifiedDateTime = DateTime.UtcNow;

        // Also revoke the associated refresh token if it exists
        if (session.RefreshTokenId.HasValue)
        {
            var refreshToken = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Id == session.RefreshTokenId.Value, ct);

            if (refreshToken != null)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = "User initiated session revoke";
            }
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Revoked session {SessionIdKey} for PersonId={PersonId}", sessionIdKey, personId);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        int personId,
        ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await changePasswordValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result.Failure(Error.FromFluentValidation(validationResult));
        }

        logger.LogInformation("Changing password for PersonId={PersonId}", personId);

        var person = await context.People
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (person == null)
        {
            return Result.Failure(Error.NotFound("Person", IdKeyHelper.Encode(personId)));
        }

        // Verify current password using AuthService's shared password verification
        if (string.IsNullOrEmpty(person.PasswordHash) ||
            !await authService.VerifyPasswordAsync(request.CurrentPassword, person.PasswordHash, ct))
        {
            return Result.Failure(Error.Validation("Current password is incorrect"));
        }

        // Hash new password
        var newPasswordHash = await authService.HashPasswordAsync(request.NewPassword);
        person.PasswordHash = newPasswordHash;
        person.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully changed password for PersonId={PersonId}", personId);

        return Result.Success();
    }

    public async Task<Result<TwoFactorStatusDto>> GetTwoFactorStatusAsync(int personId, CancellationToken ct = default)
    {
        logger.LogInformation("Getting 2FA status for PersonId={PersonId}", personId);

        var twoFactorConfig = await context.TwoFactorConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonId == personId, ct);

        var statusDto = new TwoFactorStatusDto
        {
            IsEnabled = twoFactorConfig?.IsEnabled ?? false,
            EnabledAt = twoFactorConfig?.EnabledAt
        };

        return Result<TwoFactorStatusDto>.Success(statusDto);
    }

    public async Task<Result<TwoFactorSetupDto>> SetupTwoFactorAsync(int personId, CancellationToken ct = default)
    {
        logger.LogInformation("Setting up 2FA for PersonId={PersonId}", personId);

        var person = await context.People
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (person == null)
        {
            return Result<TwoFactorSetupDto>.Failure(Error.NotFound("Person", IdKeyHelper.Encode(personId)));
        }

        // Generate a new secret key
        var secretKey = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));

        // Generate QR code URI for authenticator apps
        var issuer = "Koinon RMS";
        var accountName = person.Email ?? person.FullName;
        var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}";

        // Generate recovery codes
        var recoveryCodes = GenerateRecoveryCodes(8);

        // Hash recovery codes for storage using the same secure hashing as passwords
        var hashedRecoveryCodes = new List<string>();
        foreach (var code in recoveryCodes)
        {
            var hashed = await authService.HashPasswordAsync(code);
            hashedRecoveryCodes.Add(hashed);
        }
        var recoveryCodesJson = System.Text.Json.JsonSerializer.Serialize(hashedRecoveryCodes);

        // Create or update TwoFactorConfig
        var existingConfig = await context.TwoFactorConfigs
            .FirstOrDefaultAsync(c => c.PersonId == personId, ct);

        if (existingConfig != null)
        {
            // Update existing config (user is re-setting up 2FA)
            existingConfig.SecretKey = secretKey;
            existingConfig.RecoveryCodes = recoveryCodesJson;
            existingConfig.IsEnabled = false; // Not enabled until verified
            existingConfig.EnabledAt = null;
            existingConfig.ModifiedDateTime = DateTime.UtcNow;
        }
        else
        {
            // Create new config
            var newConfig = new TwoFactorConfig
            {
                PersonId = personId,
                SecretKey = secretKey,
                RecoveryCodes = recoveryCodesJson,
                IsEnabled = false, // Not enabled until verified
                EnabledAt = null
            };
            context.TwoFactorConfigs.Add(newConfig);
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully set up 2FA for PersonId={PersonId}", personId);

        var setupDto = new TwoFactorSetupDto
        {
            SecretKey = secretKey,
            QrCodeUri = qrCodeUri,
            RecoveryCodes = recoveryCodes
        };

        return Result<TwoFactorSetupDto>.Success(setupDto);
    }

    public async Task<Result> VerifyTwoFactorAsync(
        int personId,
        TwoFactorVerifyRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await twoFactorVerifyValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result.Failure(Error.FromFluentValidation(validationResult));
        }

        logger.LogInformation("Verifying 2FA code for PersonId={PersonId}", personId);

        var twoFactorConfig = await context.TwoFactorConfigs
            .FirstOrDefaultAsync(c => c.PersonId == personId, ct);

        if (twoFactorConfig == null)
        {
            return Result.Failure(Error.NotFound("TwoFactorConfig", IdKeyHelper.Encode(personId)));
        }

        // Verify the TOTP code
        var secretKeyBytes = Base32Encoding.ToBytes(twoFactorConfig.SecretKey);
        var totp = new Totp(secretKeyBytes);
        var isValid = totp.VerifyTotp(request.Code, out _, new VerificationWindow(2, 2));

        if (!isValid)
        {
            logger.LogWarning("Invalid 2FA code provided for PersonId={PersonId}", personId);
            return Result.Failure(Error.Validation("Invalid two-factor authentication code"));
        }

        // Enable 2FA and record when it was enabled
        twoFactorConfig.IsEnabled = true;
        twoFactorConfig.EnabledAt = DateTime.UtcNow;
        twoFactorConfig.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully verified and enabled 2FA for PersonId={PersonId}", personId);

        return Result.Success();
    }

    public async Task<Result> DisableTwoFactorAsync(
        int personId,
        TwoFactorVerifyRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await twoFactorVerifyValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result.Failure(Error.FromFluentValidation(validationResult));
        }

        logger.LogInformation("Disabling 2FA for PersonId={PersonId}", personId);

        var twoFactorConfig = await context.TwoFactorConfigs
            .FirstOrDefaultAsync(c => c.PersonId == personId, ct);

        if (twoFactorConfig == null || !twoFactorConfig.IsEnabled)
        {
            return Result.Failure(Error.NotFound("TwoFactorConfig", IdKeyHelper.Encode(personId)));
        }

        // Verify the TOTP code before disabling
        var secretKeyBytes = Base32Encoding.ToBytes(twoFactorConfig.SecretKey);
        var totp = new Totp(secretKeyBytes);
        var isValid = totp.VerifyTotp(request.Code, out _, new VerificationWindow(2, 2));

        if (!isValid)
        {
            logger.LogWarning("Invalid 2FA code provided for PersonId={PersonId}", personId);
            return Result.Failure(Error.Validation("Invalid two-factor authentication code"));
        }

        // Disable 2FA by setting IsEnabled to false
        twoFactorConfig.IsEnabled = false;
        twoFactorConfig.EnabledAt = null;
        twoFactorConfig.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully disabled 2FA for PersonId={PersonId}", personId);

        return Result.Success();
    }

    // Private helper methods

    private async Task<int?> GetCurrentRefreshTokenIdAsync(int personId, CancellationToken ct)
    {
        // This would need to be enhanced to track the current request's refresh token
        // For now, return the most recent active refresh token
        var mostRecentToken = await context.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.PersonId == personId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedDateTime)
            .FirstOrDefaultAsync(ct);

        return mostRecentToken?.Id;
    }

    private static IReadOnlyList<string> GenerateRecoveryCodes(int count)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed ambiguous chars (0, O, 1, I, L)
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            // Generate 8-character alphanumeric code
            var bytes = RandomNumberGenerator.GetBytes(8);
            var code = new char[8];
            for (int j = 0; j < 8; j++)
            {
                code[j] = chars[bytes[j] % chars.Length];
            }
            codes.Add(new string(code));
        }
        return codes;
    }
}
