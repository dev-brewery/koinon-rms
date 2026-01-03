using FluentAssertions;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for SessionCleanupService.
/// </summary>
public class SessionCleanupServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<SessionCleanupService>> _mockLogger;
    private readonly SessionCleanupService _service;

    public SessionCleanupServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"SessionCleanupTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Setup configuration mock - default 30 days retention
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration
            .Setup(c => c.GetSection("SessionCleanup:RetentionDays").Value)
            .Returns("30");

        // Setup logger mock
        _mockLogger = new Mock<ILogger<SessionCleanupService>>();

        // Create service
        _service = new SessionCleanupService(
            _context,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_NoExpiredSessions_ShouldReturnZero()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var activeSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "active-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            EndedAt = null,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.SupervisorSessions.Add(activeSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _context.SupervisorSessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_ExpiredSessionsBeyondRetention_ShouldDeleteThem()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var oldExpiredSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "old-expired-token",
            ExpiresAt = cutoffDate.AddDays(-5), // Expired 35 days ago
            EndedAt = null,
            CreatedDateTime = cutoffDate.AddDays(-5)
        };
        _context.SupervisorSessions.Add(oldExpiredSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _context.SupervisorSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_EndedSessionsBeyondRetention_ShouldDeleteThem()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var oldEndedSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "old-ended-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1), // Would still be valid
            EndedAt = cutoffDate.AddDays(-10), // But was manually ended 40 days ago
            CreatedDateTime = cutoffDate.AddDays(-40)
        };
        _context.SupervisorSessions.Add(oldEndedSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _context.SupervisorSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_RecentExpiredSessions_ShouldNotDeleteThem()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var recentExpiredSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "recent-expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-15), // Expired 15 days ago (within 30-day retention)
            EndedAt = null,
            CreatedDateTime = DateTime.UtcNow.AddDays(-15)
        };
        _context.SupervisorSessions.Add(recentExpiredSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _context.SupervisorSessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_MixedSessions_ShouldOnlyDeleteOldOnes()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        // Should be deleted
        var oldExpiredSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "old-expired-1",
            ExpiresAt = cutoffDate.AddDays(-5),
            EndedAt = null,
            CreatedDateTime = cutoffDate.AddDays(-5)
        };
        _context.SupervisorSessions.Add(oldExpiredSession);

        // Should be deleted
        var oldEndedSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "old-ended-1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            EndedAt = cutoffDate.AddDays(-10),
            CreatedDateTime = cutoffDate.AddDays(-40)
        };
        _context.SupervisorSessions.Add(oldEndedSession);

        // Should NOT be deleted (recent)
        var recentExpiredSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "recent-expired",
            ExpiresAt = DateTime.UtcNow.AddDays(-15),
            EndedAt = null,
            CreatedDateTime = DateTime.UtcNow.AddDays(-15)
        };
        _context.SupervisorSessions.Add(recentExpiredSession);

        // Should NOT be deleted (active)
        var activeSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "active",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            EndedAt = null,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.SupervisorSessions.Add(activeSession);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(2);
        _context.SupervisorSessions.Should().HaveCount(2);

        var remainingSessions = await _context.SupervisorSessions.ToListAsync();
        remainingSessions.Should().Contain(s => s.Token == "recent-expired");
        remainingSessions.Should().Contain(s => s.Token == "active");
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_CustomRetentionDays_ShouldUseConfiguredValue()
    {
        // Arrange
        var customRetentionDays = 60;
        var customConfig = new Mock<IConfiguration>();
        customConfig
            .Setup(c => c.GetSection("SessionCleanup:RetentionDays").Value)
            .Returns(customRetentionDays.ToString());

        var customService = new SessionCleanupService(
            _context,
            customConfig.Object,
            _mockLogger.Object);

        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-60);

        // Should be deleted (older than 60 days)
        var veryOldSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "very-old",
            ExpiresAt = cutoffDate.AddDays(-5),
            EndedAt = null,
            CreatedDateTime = cutoffDate.AddDays(-5)
        };
        _context.SupervisorSessions.Add(veryOldSession);

        // Should NOT be deleted (within 60 days)
        var oldButNotVeryOldSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "old-but-not-very-old",
            ExpiresAt = DateTime.UtcNow.AddDays(-45),
            EndedAt = null,
            CreatedDateTime = DateTime.UtcNow.AddDays(-45)
        };
        _context.SupervisorSessions.Add(oldButNotVeryOldSession);

        await _context.SaveChangesAsync();

        // Act
        var result = await customService.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _context.SupervisorSessions.Should().HaveCount(1);
        var remainingSession = await _context.SupervisorSessions.SingleAsync();
        remainingSession.Token.Should().Be("old-but-not-very-old");
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_NoConfiguration_ShouldUseDefault30Days()
    {
        // Arrange
        var defaultConfig = new Mock<IConfiguration>();
        defaultConfig
            .Setup(c => c.GetSection("SessionCleanup:RetentionDays").Value)
            .Returns((string?)null); // No configuration

        var defaultService = new SessionCleanupService(
            _context,
            defaultConfig.Object,
            _mockLogger.Object);

        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var oldSession = new SupervisorSession
        {
            PersonId = person.Id,
            Token = "old-session",
            ExpiresAt = cutoffDate.AddDays(-1),
            EndedAt = null,
            CreatedDateTime = cutoffDate.AddDays(-1)
        };
        _context.SupervisorSessions.Add(oldSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await defaultService.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _context.SupervisorSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_EmptyDatabase_ShouldReturnZero()
    {
        // Arrange - no sessions in database

        // Act
        var result = await _service.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_MultipleOldSessions_ShouldDeleteAll()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            RecordStatusValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        // Add 10 old sessions
        for (int i = 0; i < 10; i++)
        {
            var oldSession = new SupervisorSession
            {
                PersonId = person.Id,
                Token = $"old-session-{i}",
                ExpiresAt = cutoffDate.AddDays(-i - 1),
                EndedAt = null,
                CreatedDateTime = cutoffDate.AddDays(-i - 1)
            };
            _context.SupervisorSessions.Add(oldSession);
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CleanupExpiredSessionsAsync(CancellationToken.None);

        // Assert
        result.Should().Be(10);
        _context.SupervisorSessions.Should().BeEmpty();
    }
}
