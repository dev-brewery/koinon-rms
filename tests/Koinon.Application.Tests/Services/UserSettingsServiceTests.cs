using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
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

    // Test DbContext for in-memory testing
    private class TestDbContext : DbContext, IApplicationDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Person> People { get; set; } = null!;
        public DbSet<PersonAlias> PersonAliases { get; set; } = null!;
        public DbSet<PhoneNumber> PhoneNumbers { get; set; } = null!;
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
