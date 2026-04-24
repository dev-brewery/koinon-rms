using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OtpNet;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Unit tests for UserSettingsService 2FA functionality.
/// </summary>
public class UserSettingsServiceTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IValidator<UpdateUserPreferenceRequest>> _updatePreferenceValidatorMock;
    private readonly Mock<IValidator<ChangePasswordRequest>> _changePasswordValidatorMock;
    private readonly Mock<IValidator<TwoFactorVerifyRequest>> _twoFactorVerifyValidatorMock;
    private readonly Mock<ILogger<UserSettingsService>> _loggerMock;

    public UserSettingsServiceTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _updatePreferenceValidatorMock = new Mock<IValidator<UpdateUserPreferenceRequest>>();
        _changePasswordValidatorMock = new Mock<IValidator<ChangePasswordRequest>>();
        _twoFactorVerifyValidatorMock = new Mock<IValidator<TwoFactorVerifyRequest>>();
        _loggerMock = new Mock<ILogger<UserSettingsService>>();

        // Setup default validation results (all valid)
        _updatePreferenceValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateUserPreferenceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _changePasswordValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _twoFactorVerifyValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<TwoFactorVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private IApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>
    /// Creates an in-memory DbContext configured with NoTracking as the global default,
    /// mirroring the production PostgreSqlProvider configuration. This is required for
    /// bug-regression tests (#708): tests against a tracking DbContext will silently pass
    /// against buggy code because the in-memory entity reference is the same as what a
    /// subsequent FirstOrDefaultAsync returns (it's the tracked instance, not a fresh
    /// DB read).
    /// </summary>
    private static TestDbContext CreateNoTrackingInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>
    /// Helper method to create a valid password hash for testing.
    /// Mimics the AuthService.HashPasswordAsync method.
    /// </summary>
    private static async Task<string> CreatePasswordHashAsync(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            Iterations = 4,
            MemorySize = 128 * 1024
        };

        var hash = await argon2.GetBytesAsync(32);

        var combined = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);

        return Convert.ToBase64String(combined);
    }

    #region GetTwoFactorStatusAsync Tests

    [Fact]
    public async Task GetTwoFactorStatusAsync_WhenNoConfig_ReturnsDisabled()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GetTwoFactorStatusAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsEnabled);
        Assert.Null(result.Value.EnabledAt);
    }

    [Fact]
    public async Task GetTwoFactorStatusAsync_WhenConfigExists_ReturnsStatus()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var enabledAt = DateTime.UtcNow.AddDays(-7);
        var config = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20)),
            IsEnabled = true,
            EnabledAt = enabledAt
        };
        context.TwoFactorConfigs.Add(config);
        await context.SaveChangesAsync();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GetTwoFactorStatusAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsEnabled);
        Assert.Equal(enabledAt, result.Value.EnabledAt);
    }

    #endregion

    #region SetupTwoFactorAsync Tests

    [Fact]
    public async Task SetupTwoFactorAsync_WhenPersonNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.SetupTwoFactorAsync(99999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task SetupTwoFactorAsync_GeneratesSecretKeyAndQrCode()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        // Mock password hashing for recovery codes
        _authServiceMock
            .Setup(x => x.HashPasswordAsync(It.IsAny<string>()))
            .Returns<string>(async code => await CreatePasswordHashAsync(code));

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.SetupTwoFactorAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // Verify secret key is Base32 encoded (20 bytes = 32 Base32 characters)
        Assert.NotEmpty(result.Value.SecretKey);
        Assert.Equal(32, result.Value.SecretKey.Length);

        // Verify QR code URI format
        Assert.StartsWith("otpauth://totp/Koinon%20RMS:", result.Value.QrCodeUri);
        Assert.Contains($"secret={result.Value.SecretKey}", result.Value.QrCodeUri);
        Assert.Contains("issuer=Koinon%20RMS", result.Value.QrCodeUri);

        // Verify recovery codes generated
        Assert.Equal(8, result.Value.RecoveryCodes.Count);
        Assert.All(result.Value.RecoveryCodes, code => Assert.Equal(8, code.Length));
    }

    [Fact]
    public async Task SetupTwoFactorAsync_CreatesConfigWithIsEnabledFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        _authServiceMock
            .Setup(x => x.HashPasswordAsync(It.IsAny<string>()))
            .Returns<string>(async code => await CreatePasswordHashAsync(code));

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.SetupTwoFactorAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var config = await context.TwoFactorConfigs.FirstOrDefaultAsync(c => c.PersonId == person.Id);
        Assert.NotNull(config);
        Assert.False(config.IsEnabled); // Should not be enabled until verified
        Assert.Null(config.EnabledAt);
        Assert.NotNull(config.RecoveryCodes);
    }

    [Fact]
    public async Task SetupTwoFactorAsync_UpdatesExistingConfig()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var oldSecretKey = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var oldConfig = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = oldSecretKey,
            IsEnabled = true,
            EnabledAt = DateTime.UtcNow.AddDays(-7)
        };
        context.TwoFactorConfigs.Add(oldConfig);
        await context.SaveChangesAsync();

        _authServiceMock
            .Setup(x => x.HashPasswordAsync(It.IsAny<string>()))
            .Returns<string>(async code => await CreatePasswordHashAsync(code));

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.SetupTwoFactorAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var config = await context.TwoFactorConfigs.FirstOrDefaultAsync(c => c.PersonId == person.Id);
        Assert.NotNull(config);
        Assert.NotEqual(oldSecretKey, config.SecretKey); // Should have new secret
        Assert.False(config.IsEnabled); // Should be reset to disabled
        Assert.Null(config.EnabledAt); // Should be reset
    }

    #endregion

    #region VerifyTwoFactorAsync Tests

    [Fact]
    public async Task VerifyTwoFactorAsync_WhenConfigNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = "123456" };

        // Act
        var result = await service.VerifyTwoFactorAsync(person.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task VerifyTwoFactorAsync_WithInvalidCode_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var secretKey = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var config = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = secretKey,
            IsEnabled = false
        };
        context.TwoFactorConfigs.Add(config);
        await context.SaveChangesAsync();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = "000000" }; // Invalid code

        // Act
        var result = await service.VerifyTwoFactorAsync(person.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
    }

    [Fact]
    public async Task VerifyTwoFactorAsync_WithValidCode_EnablesTwoFactor()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var secretBytes = RandomNumberGenerator.GetBytes(20);
        var secretKey = Base32Encoding.ToString(secretBytes);
        var config = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = secretKey,
            IsEnabled = false
        };
        context.TwoFactorConfigs.Add(config);
        await context.SaveChangesAsync();

        // Generate valid TOTP code
        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = validCode };

        // Act
        var result = await service.VerifyTwoFactorAsync(person.Id, request);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedConfig = await context.TwoFactorConfigs.FirstOrDefaultAsync(c => c.PersonId == person.Id);
        Assert.NotNull(updatedConfig);
        Assert.True(updatedConfig.IsEnabled);
        Assert.NotNull(updatedConfig.EnabledAt);
        Assert.True(updatedConfig.EnabledAt.Value > DateTime.UtcNow.AddMinutes(-1));
    }

    #endregion

    #region DisableTwoFactorAsync Tests

    [Fact]
    public async Task DisableTwoFactorAsync_WhenConfigNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = "123456" };

        // Act
        var result = await service.DisableTwoFactorAsync(person.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_WithInvalidCode_ReturnsValidationError()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var secretKey = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var config = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = secretKey,
            IsEnabled = true,
            EnabledAt = DateTime.UtcNow.AddDays(-1)
        };
        context.TwoFactorConfigs.Add(config);
        await context.SaveChangesAsync();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = "000000" }; // Invalid code

        // Act
        var result = await service.DisableTwoFactorAsync(person.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_WithValidCode_DisablesTwoFactor()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var secretBytes = RandomNumberGenerator.GetBytes(20);
        var secretKey = Base32Encoding.ToString(secretBytes);
        var config = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = secretKey,
            IsEnabled = true,
            EnabledAt = DateTime.UtcNow.AddDays(-1)
        };
        context.TwoFactorConfigs.Add(config);
        await context.SaveChangesAsync();

        // Generate valid TOTP code
        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = validCode };

        // Act
        var result = await service.DisableTwoFactorAsync(person.Id, request);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedConfig = await context.TwoFactorConfigs.FirstOrDefaultAsync(c => c.PersonId == person.Id);
        Assert.NotNull(updatedConfig);
        Assert.False(updatedConfig.IsEnabled);
        Assert.Null(updatedConfig.EnabledAt);
    }

    #endregion

    // ---------------------------------------------------------------------
    // Bug #708 regression tests — SECURITY-CRITICAL.
    //
    // Production configures PostgreSqlProvider with QueryTrackingBehavior.NoTracking,
    // which means FirstOrDefaultAsync/FindAsync return DETACHED entities. Without an
    // explicit .AsTracking() on the load query, mutations to the returned entity are
    // silently dropped on SaveChanges — leaving the database row unchanged while the
    // API happily returns success.
    //
    // For 2FA flows this is particularly dangerous: a silently-dropped Enable2FA would
    // let a user believe they're protected when they aren't.
    //
    // Each of these tests:
    //   1. Uses a DbContext with NoTracking as the global default (mirroring prod).
    //   2. Mutates via the service, SaveChanges.
    //   3. Detaches all tracked entities (ChangeTracker.Clear) so the reload is a fresh
    //      DB read — NOT a tracker cache hit.
    //   4. Reloads with AsNoTracking and asserts the mutation actually persisted.
    //
    // These tests FAIL against the pre-fix service and PASS after the fix.
    // ---------------------------------------------------------------------

    #region Bug #708 — 2FA SECURITY persistence under NoTracking

    [Fact]
    public async Task VerifyTwoFactorAsync_UnderNoTracking_PersistsEnableToDatabase()
    {
        // Arrange
        using var context = CreateNoTrackingInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var secretBytes = RandomNumberGenerator.GetBytes(20);
        var secretKey = Base32Encoding.ToString(secretBytes);
        var config = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = secretKey,
            IsEnabled = false
        };
        context.TwoFactorConfigs.Add(config);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = validCode };

        // Act
        var result = await service.VerifyTwoFactorAsync(person.Id, request);

        // Assert — the API returned success.
        Assert.True(result.IsSuccess);

        // Assert — and the DB actually has IsEnabled=true. This is the critical check:
        // pre-fix, the mutation was silently dropped and this assertion would fail.
        context.ChangeTracker.Clear();
        var reloaded = await context.TwoFactorConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PersonId == person.Id);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsEnabled,
            "2FA IsEnabled must be persisted to the database; if false, the user believes they are protected when they are not.");
        Assert.NotNull(reloaded.EnabledAt);
    }

    [Fact]
    public async Task DisableTwoFactorAsync_UnderNoTracking_PersistsDisableToDatabase()
    {
        // Arrange
        using var context = CreateNoTrackingInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var secretBytes = RandomNumberGenerator.GetBytes(20);
        var secretKey = Base32Encoding.ToString(secretBytes);
        var config = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = secretKey,
            IsEnabled = true,
            EnabledAt = DateTime.UtcNow.AddDays(-1)
        };
        context.TwoFactorConfigs.Add(config);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new TwoFactorVerifyRequest { Code = validCode };

        // Act
        var result = await service.DisableTwoFactorAsync(person.Id, request);

        // Assert
        Assert.True(result.IsSuccess);

        context.ChangeTracker.Clear();
        var reloaded = await context.TwoFactorConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PersonId == person.Id);
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsEnabled,
            "2FA IsEnabled must be persisted as false after disable; if still true, 2FA remains active despite user request to disable.");
        Assert.Null(reloaded.EnabledAt);
    }

    [Fact]
    public async Task SetupTwoFactorAsync_UnderNoTracking_PersistsSecretForExistingConfig()
    {
        // Arrange — user already has a 2FA config (previously enabled) and is re-setting up.
        using var context = CreateNoTrackingInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var existing = new TwoFactorConfig
        {
            PersonId = person.Id,
            SecretKey = "OLD_SECRET_KEY_THAT_SHOULD_BE_REPLACED",
            IsEnabled = true,
            EnabledAt = DateTime.UtcNow.AddDays(-1)
        };
        context.TwoFactorConfigs.Add(existing);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        _authServiceMock
            .Setup(a => a.HashPasswordAsync(It.IsAny<string>()))
            .ReturnsAsync((string s) => $"hashed-{s}");

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.SetupTwoFactorAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);

        context.ChangeTracker.Clear();
        var reloaded = await context.TwoFactorConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PersonId == person.Id);
        Assert.NotNull(reloaded);
        Assert.NotEqual("OLD_SECRET_KEY_THAT_SHOULD_BE_REPLACED", reloaded.SecretKey);
        Assert.Equal(result.Value!.SecretKey, reloaded.SecretKey);
        Assert.False(reloaded.IsEnabled); // not enabled until verified
        Assert.Null(reloaded.EnabledAt);
    }

    [Fact]
    public async Task ChangePasswordAsync_UnderNoTracking_PersistsNewHashToDatabase()
    {
        // Arrange
        using var context = CreateNoTrackingInMemoryContext();
        var oldHash = await CreatePasswordHashAsync("OldPass123!");
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = oldHash
        };
        context.People.Add(person);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        _authServiceMock
            .Setup(a => a.VerifyPasswordAsync("OldPass123!", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _authServiceMock
            .Setup(a => a.HashPasswordAsync("NewPass456!"))
            .ReturnsAsync("brand-new-hash");

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456!",
            ConfirmPassword = "NewPass456!"
        };

        // Act
        var result = await service.ChangePasswordAsync(person.Id, request);

        // Assert — API returned success.
        Assert.True(result.IsSuccess);

        // Assert — the DB actually has the new hash. Pre-fix, the PasswordHash mutation
        // was silently dropped and the user's password would be unchanged even though the
        // API returned success.
        context.ChangeTracker.Clear();
        var reloaded = await context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == person.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("brand-new-hash", reloaded.PasswordHash);
        Assert.NotEqual(oldHash, reloaded.PasswordHash);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_UnderNoTracking_PersistsExistingConfigUpdate()
    {
        // Arrange
        using var context = CreateNoTrackingInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var existing = new UserPreference
        {
            PersonId = person.Id,
            Theme = Theme.Light,
            DateFormat = "MM/dd/yyyy",
            TimeZone = "America/New_York"
        };
        context.UserPreferences.Add(existing);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);
        var request = new UpdateUserPreferenceRequest
        {
            Theme = Theme.Dark,
            DateFormat = "yyyy-MM-dd",
            TimeZone = "UTC"
        };

        // Act
        var result = await service.UpdatePreferencesAsync(person.Id, request);

        // Assert
        Assert.True(result.IsSuccess);

        context.ChangeTracker.Clear();
        var reloaded = await context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == person.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(Theme.Dark, reloaded.Theme);
        Assert.Equal("yyyy-MM-dd", reloaded.DateFormat);
        Assert.Equal("UTC", reloaded.TimeZone);
    }

    [Fact]
    public async Task RevokeSessionAsync_UnderNoTracking_PersistsSessionInactiveAndRefreshTokenRevoked()
    {
        // Arrange — SECURITY-CRITICAL: a failed revoke means the session stays active.
        using var context = CreateNoTrackingInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            PersonId = person.Id,
            Token = "a-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var session = new UserSession
        {
            PersonId = person.Id,
            RefreshTokenId = refreshToken.Id,
            IsActive = true,
            LastActivityAt = DateTime.UtcNow,
            DeviceInfo = "Test Device",
            IpAddress = "127.0.0.1"
        };
        context.UserSessions.Add(session);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var sessionIdKey = IdKeyHelper.Encode(session.Id);
        var service = new UserSettingsService(
            context,
            _authServiceMock.Object,
            _updatePreferenceValidatorMock.Object,
            _changePasswordValidatorMock.Object,
            _twoFactorVerifyValidatorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.RevokeSessionAsync(person.Id, sessionIdKey);

        // Assert
        Assert.True(result.IsSuccess);

        context.ChangeTracker.Clear();
        var reloadedSession = await context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == session.Id);
        Assert.NotNull(reloadedSession);
        Assert.False(reloadedSession.IsActive,
            "Session must be persisted as inactive after revoke; if still active, a user-initiated revoke was a silent no-op.");

        var reloadedToken = await context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == refreshToken.Id);
        Assert.NotNull(reloadedToken);
        Assert.NotNull(reloadedToken.RevokedAt);
    }

    #endregion

    // Test DbContext for in-memory testing
    private class TestDbContext : DbContext, IApplicationDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Person> People { get; set; } = null!;
        public DbSet<PersonAlias> PersonAliases { get; set; } = null!;
        public DbSet<PhoneNumber> PhoneNumbers { get; set; } = null!;
        public DbSet<PersonNote> PersonNotes { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<GroupType> GroupTypes { get; set; } = null!;
        public DbSet<GroupTypeRole> GroupTypeRoles { get; set; } = null!;
        public DbSet<GroupMember> GroupMembers { get; set; } = null!;
        public DbSet<GroupMemberRequest> GroupMemberRequests { get; set; } = null!;
        public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
        public DbSet<Family> Families { get; set; } = null!;
        public DbSet<GroupSchedule> GroupSchedules { get; set; } = null!;
        public DbSet<Campus> Campuses { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<DefinedType> DefinedTypes { get; set; } = null!;
        public DbSet<DefinedValue> DefinedValues { get; set; } = null!;
        public DbSet<Schedule> Schedules { get; set; } = null!;
        public DbSet<Attendance> Attendances { get; set; } = null!;
        public DbSet<AttendanceOccurrence> AttendanceOccurrences { get; set; } = null!;
        public DbSet<AttendanceCode> AttendanceCodes { get; set; } = null!;
        public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<LabelTemplate> LabelTemplates { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<UserPreference> UserPreferences { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;
        public DbSet<TwoFactorConfig> TwoFactorConfigs { get; set; } = null!;
        public DbSet<SupervisorSession> SupervisorSessions { get; set; } = null!;
        public DbSet<SupervisorAuditLog> SupervisorAuditLogs { get; set; } = null!;
        public DbSet<FollowUp> FollowUps { get; set; } = null!;
        public DbSet<PagerAssignment> PagerAssignments { get; set; } = null!;
        public DbSet<PagerMessage> PagerMessages { get; set; } = null!;
        public DbSet<AuthorizedPickup> AuthorizedPickups { get; set; } = null!;
        public DbSet<PickupLog> PickupLogs { get; set; } = null!;
        public DbSet<Communication> Communications { get; set; } = null!;
        public DbSet<CommunicationRecipient> CommunicationRecipients { get; set; } = null!;
        public DbSet<CommunicationTemplate> CommunicationTemplates { get; set; } = null!;
        public DbSet<CommunicationPreference> CommunicationPreferences { get; set; } = null!;
        public DbSet<BinaryFile> BinaryFiles { get; set; } = null!;
        public DbSet<ImportTemplate> ImportTemplates { get; set; } = null!;
        public DbSet<ImportJob> ImportJobs { get; set; } = null!;
        public DbSet<ReportDefinition> ReportDefinitions { get; set; } = null!;
        public DbSet<ReportRun> ReportRuns { get; set; } = null!;
        public DbSet<ReportSchedule> ReportSchedules { get; set; } = null!;
        public DbSet<ExportJob> ExportJobs { get; set; } = null!;

        // Giving/Financial
        public DbSet<Fund> Funds { get; set; } = null!;
        public DbSet<ContributionBatch> ContributionBatches { get; set; } = null!;
        public DbSet<Contribution> Contributions { get; set; } = null!;
        public DbSet<ContributionDetail> ContributionDetails { get; set; } = null!;
        public DbSet<ContributionStatement> ContributionStatements { get; set; } = null!;
        public DbSet<FinancialAuditLog> FinancialAuditLogs { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        // Person merge and duplicate tracking
        public DbSet<PersonMergeHistory> PersonMergeHistories { get; set; } = null!;
        public DbSet<PersonDuplicateIgnore> PersonDuplicateIgnores { get; set; } = null!;

        // In-app notifications
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;

        // Security
        public DbSet<SecurityRole> SecurityRoles { get; set; } = null!;
        public DbSet<SecurityClaim> SecurityClaims { get; set; } = null!;
        public DbSet<PersonSecurityRole> PersonSecurityRoles { get; set; } = null!;
        public DbSet<RoleSecurityClaim> RoleSecurityClaims { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Location self-referential relationships
            modelBuilder.Entity<Location>()
                .HasOne(l => l.ParentLocation)
                .WithMany(l => l.ChildLocations)
                .HasForeignKey(l => l.ParentLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Location>()
                .HasOne(l => l.OverflowLocation)
                .WithMany()
                .HasForeignKey(l => l.OverflowLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
