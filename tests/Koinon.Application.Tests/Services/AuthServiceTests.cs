using System.Security.Cryptography;
using System.Text;
using Koinon.Application.DTOs.Auth;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Unit tests for AuthService.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        _loggerMock = new Mock<ILogger<AuthService>>();

        // Create in-memory configuration
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-key-that-is-at-least-32-characters-long-for-testing",
            ["Jwt:Issuer"] = "Koinon.Api.Test",
            ["Jwt:Audience"] = "Koinon.Web.Test",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
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

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var passwordHash = await CreatePasswordHashAsync("password123");
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = passwordHash
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _configuration, _loggerMock.Object);
        var request = new LoginRequest("test@example.com", "password123");

        // Act
        var result = await authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(person.FirstName, result.User.FirstName);
        Assert.Equal(person.LastName, result.User.LastName);
        Assert.Equal(person.Email, result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var authService = new AuthService(context, _configuration, _loggerMock.Object);
        var request = new LoginRequest("nonexistent@example.com", "password123");

        // Act
        var result = await authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var passwordHash = await CreatePasswordHashAsync("correctpassword");
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = passwordHash
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _configuration, _loggerMock.Object);
        var request = new LoginRequest("test@example.com", "wrongpassword");

        // Act
        var result = await authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithNullPasswordHash_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = null // No password set
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _configuration, _loggerMock.Object);
        var request = new LoginRequest("test@example.com", "anypassword");

        // Act
        var result = await authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokenResponse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var passwordHash = await CreatePasswordHashAsync("password123");
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = passwordHash
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        // Generate a valid Base64 token (64 bytes)
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var validToken = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            PersonId = person.Id,
            Token = validToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1"
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _configuration, _loggerMock.Object);

        // Act
        var result = await authService.RefreshTokenAsync(validToken, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEqual(validToken, result.RefreshToken); // Should be rotated
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var authService = new AuthService(context, _configuration, _loggerMock.Object);

        // Act
        var result = await authService.RefreshTokenAsync("invalid-token", "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var passwordHash = await CreatePasswordHashAsync("password123");
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = passwordHash
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        // Generate a valid Base64 token (64 bytes)
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var expiredTokenString = Convert.ToBase64String(tokenBytes);

        var expiredToken = new RefreshToken
        {
            PersonId = person.Id,
            Token = expiredTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedByIp = "127.0.0.1"
        };
        context.RefreshTokens.Add(expiredToken);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _configuration, _loggerMock.Object);

        // Act
        var result = await authService.RefreshTokenAsync(expiredTokenString, "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LogoutAsync_WithValidToken_RevokesToken()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var passwordHash = await CreatePasswordHashAsync("password123");
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = passwordHash
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        // Generate a valid Base64 token (64 bytes)
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var tokenToRevoke = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            PersonId = person.Id,
            Token = tokenToRevoke,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1"
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _configuration, _loggerMock.Object);

        // Act
        var result = await authService.LogoutAsync(tokenToRevoke, "127.0.0.1");

        // Assert
        Assert.True(result);

        var revokedToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == tokenToRevoke);
        Assert.NotNull(revokedToken);
        Assert.NotNull(revokedToken.RevokedAt);
        Assert.Equal("127.0.0.1", revokedToken.RevokedByIp);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var authService = new AuthService(context, _configuration, _loggerMock.Object);

        // Act
        var result = await authService.LogoutAsync("invalid-token", "127.0.0.1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task LoginAsync_StoresRefreshTokenInDatabase()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var passwordHash = await CreatePasswordHashAsync("password123");
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Gender = Gender.Unknown,
            PasswordHash = passwordHash
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _configuration, _loggerMock.Object);
        var request = new LoginRequest("test@example.com", "password123");

        // Act
        var result = await authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.NotNull(result);

        var storedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == result.RefreshToken);

        Assert.NotNull(storedToken);
        Assert.Equal(person.Id, storedToken.PersonId);
        Assert.Equal("127.0.0.1", storedToken.CreatedByIp);
        Assert.True(storedToken.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task HashPasswordAsync_CreatesValidHash()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var authService = new AuthService(context, _configuration, _loggerMock.Object);

        // Act
        var hash1 = await authService.HashPasswordAsync("testpassword");
        var hash2 = await authService.HashPasswordAsync("testpassword");

        // Assert
        Assert.NotNull(hash1);
        Assert.NotNull(hash2);
        Assert.NotEqual(hash1, hash2); // Different salts should produce different hashes

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(hash1);
        Assert.Equal(48, bytes.Length); // 16 salt + 32 hash
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidFormat_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var authService = new AuthService(context, _configuration, _loggerMock.Object);

        // Act - Test with various invalid formats
        var result1 = await authService.RefreshTokenAsync("not-base64!@#$", "127.0.0.1");
        var result2 = await authService.RefreshTokenAsync("", "127.0.0.1");
        var result3 = await authService.RefreshTokenAsync("    ", "127.0.0.1");

        // Valid Base64 but wrong length (not 64 bytes)
        var shortToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var result4 = await authService.RefreshTokenAsync(shortToken, "127.0.0.1");

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.Null(result3);
        Assert.Null(result4);
    }

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
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<SupervisorSession> SupervisorSessions { get; set; } = null!;
        public DbSet<SupervisorAuditLog> SupervisorAuditLogs { get; set; } = null!;
    }
}
