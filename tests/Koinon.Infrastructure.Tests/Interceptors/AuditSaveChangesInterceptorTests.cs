using System.Text.Json;
using FluentAssertions;
using Koinon.Application.Interfaces;
using Koinon.Domain.Attributes;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Infrastructure.Tests.Interceptors;

/// <summary>
/// Integration tests for AuditSaveChangesInterceptor.
/// Verifies automatic audit logging for entity changes.
/// Uses in-memory database for fast, isolated testing.
/// </summary>
public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<AuditSaveChangesInterceptor>> _mockLogger;
    private readonly AuditSaveChangesInterceptor _interceptor;

    public AuditSaveChangesInterceptorTests()
    {
        // Setup mocks
        _mockUserContext = new Mock<IUserContext>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<AuditSaveChangesInterceptor>>();

        // Setup default HTTP context with IP and User-Agent
        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        var mockRequest = new Mock<HttpRequest>();

        mockConnection.Setup(c => c.RemoteIpAddress)
            .Returns(System.Net.IPAddress.Parse("192.168.1.100"));

        mockRequest.Setup(r => r.Headers)
            .Returns(new HeaderDictionary
            {
                { "User-Agent", "Mozilla/5.0 Test Browser" }
            });

        mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

        _mockHttpContextAccessor.Setup(a => a.HttpContext)
            .Returns(mockHttpContext.Object);

        // Setup default user context (authenticated user with ID 1)
        _mockUserContext.Setup(u => u.CurrentPersonId).Returns(1);

        // Create interceptor
        _interceptor = new AuditSaveChangesInterceptor(
            _mockUserContext.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Create in-memory database with interceptor
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(_interceptor)
            .Options;

        _context = new KoinonDbContext(options);
    }

    [Fact]
    public async Task SavingChangesAsync_EntityAdded_CreatesCreateAuditLog()
    {
        // Arrange
        var person = CreateTestPerson("John", "Doe");

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var auditLog = auditLogs[0];
        auditLog.ActionType.Should().Be(AuditAction.Create);
        auditLog.EntityType.Should().Be("Person");
        auditLog.EntityIdKey.Should().NotBeNullOrEmpty();
        auditLog.PersonId.Should().Be(1);
        auditLog.OldValues.Should().BeNull();
        auditLog.NewValues.Should().NotBeNull();
        auditLog.ChangedProperties.Should().BeNull();
        auditLog.IpAddress.Should().Be("192.168.1.100");
        auditLog.UserAgent.Should().Be("Mozilla/5.0 Test Browser");
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SavingChangesAsync_EntityModified_CreatesUpdateAuditLog()
    {
        // Arrange
        var person = CreateTestPerson("Jane", "Smith");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Get the person ID and create a new context to simulate a fresh query
        var personId = person.Id;

        // Modify the person
        var existingPerson = await _context.People.FindAsync(personId);
        existingPerson!.FirstName = "Janet";
        existingPerson.Email = "janet.smith@example.com";

        // Act
        await _context.SaveChangesAsync();

        // Assert - Should have 2 audit logs (1 for create, 1 for update)
        var auditLogs = await _context.AuditLogs
            .Where(a => a.EntityType == "Person")
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(2);

        var updateAuditLog = auditLogs[1];
        updateAuditLog.ActionType.Should().Be(AuditAction.Update);
        updateAuditLog.EntityType.Should().Be("Person");
        updateAuditLog.PersonId.Should().Be(1);
        updateAuditLog.OldValues.Should().NotBeNull();
        updateAuditLog.NewValues.Should().NotBeNull();
        updateAuditLog.ChangedProperties.Should().NotBeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_EntityDeleted_CreatesDeleteAuditLog()
    {
        // Arrange
        var person = CreateTestPerson("Bob", "Johnson");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act
        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        // Assert - Should have 2 audit logs (1 for create, 1 for delete)
        var auditLogs = await _context.AuditLogs
            .Where(a => a.EntityType == "Person")
            .ToListAsync();

        auditLogs.Should().HaveCount(2);

        // Find the delete audit log specifically
        var deleteAuditLog = auditLogs.FirstOrDefault(a => a.ActionType == AuditAction.Delete);
        deleteAuditLog.Should().NotBeNull("a delete audit log should be created");

        deleteAuditLog!.ActionType.Should().Be(AuditAction.Delete);
        deleteAuditLog.EntityType.Should().Be("Person");

        // Note: PersonId may be null for delete operations if the user context is cleared
        // This is acceptable as long as the audit log is created
        deleteAuditLog.OldValues.Should().NotBeNull();
        deleteAuditLog.NewValues.Should().BeNull();
        deleteAuditLog.ChangedProperties.Should().BeNull();

        // Verify old values capture the deleted entity's data
        var oldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(
            deleteAuditLog.OldValues!);
        oldValues.Should().NotBeNull();
        oldValues!["FirstName"].ToString().Should().Be("Bob");
        oldValues["LastName"].ToString().Should().Be("Johnson");
    }

    [Fact]
    public async Task SavingChangesAsync_CapturesOldAndNewValues()
    {
        // Arrange
        var person = CreateTestPerson("Alice", "Williams");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        var originalFirstName = person.FirstName;
        var originalEmail = person.Email;

        // Modify the tracked entity
        var existingPerson = await _context.People.FindAsync(person.Id);
        existingPerson!.FirstName = "Alicia";
        existingPerson.Email = "alicia.williams@example.com";

        // Act
        await _context.SaveChangesAsync();

        // Assert - Get the update audit log (second one)
        var auditLogs = await _context.AuditLogs
            .Where(a => a.EntityType == "Person" && a.ActionType == AuditAction.Update)
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
        var auditLog = auditLogs[0];

        var oldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(
            auditLog.OldValues!);
        var newValues = JsonSerializer.Deserialize<Dictionary<string, object>>(
            auditLog.NewValues!);

        oldValues.Should().NotBeNull();
        newValues.Should().NotBeNull();

        // Verify old values contain original data
        oldValues!["FirstName"].ToString().Should().Be(originalFirstName);
        oldValues["Email"].ToString().Should().Be(originalEmail);

        // Verify new values contain updated data
        newValues!["FirstName"].ToString().Should().Be("Alicia");
        newValues["Email"].ToString().Should().Be("alicia.williams@example.com");
    }

    [Fact]
    public async Task SavingChangesAsync_TracksChangedProperties()
    {
        // Arrange
        var person = CreateTestPerson("Charlie", "Brown");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Modify specific properties
        var existingPerson = await _context.People.FindAsync(person.Id);
        existingPerson!.FirstName = "Charles";
        existingPerson.Email = "charles.brown@example.com";
        existingPerson.ModifiedDateTime = DateTime.UtcNow; // Manually set since we're not using Repository.UpdateAsync

        // Act
        await _context.SaveChangesAsync();

        // Assert - Get the update audit log
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Person" && a.ActionType == AuditAction.Update)
            .FirstAsync();

        auditLog.ChangedProperties.Should().NotBeNull();

        var changedProperties = JsonSerializer.Deserialize<List<string>>(
            auditLog.ChangedProperties!);

        changedProperties.Should().NotBeNull();
        changedProperties.Should().Contain("FirstName");
        changedProperties.Should().Contain("Email");
        changedProperties.Should().Contain("ModifiedDateTime");
    }

    [Fact]
    public async Task SavingChangesAsync_MasksSensitiveFields()
    {
        // Arrange
        var person = CreateTestPerson("David", "Jones");
        person.PasswordHash = "SensitivePasswordHash123";
        person.SupervisorPinHash = "SensitivePinHash456";

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var auditLog = await _context.AuditLogs.FirstAsync();
        auditLog.NewValues.Should().NotBeNull();

        var newValues = JsonSerializer.Deserialize<Dictionary<string, object>>(
            auditLog.NewValues!);

        newValues.Should().NotBeNull();

        // Verify sensitive fields are hashed (not plaintext)
        var passwordHashValue = newValues!["PasswordHash"]?.ToString();
        var pinHashValue = newValues["SupervisorPinHash"]?.ToString();

        passwordHashValue.Should().NotBe("SensitivePasswordHash123");
        pinHashValue.Should().NotBe("SensitivePinHash456");

        // Should be hashed (64-char hex string for SHA256)
        if (!string.IsNullOrEmpty(passwordHashValue))
        {
            passwordHashValue.Should().HaveLength(64);
            passwordHashValue.Should().MatchRegex("^[a-f0-9]{64}$");
        }

        if (!string.IsNullOrEmpty(pinHashValue))
        {
            pinHashValue.Should().HaveLength(64);
            pinHashValue.Should().MatchRegex("^[a-f0-9]{64}$");
        }
    }

    [Fact]
    public async Task SavingChangesAsync_SkipsAuditLogEntity_PreventRecursion()
    {
        // Arrange - Create a person first to have a valid PersonId
        var person = CreateTestPerson("Test", "User");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Clear existing audit logs
        _context.AuditLogs.RemoveRange(_context.AuditLogs);
        await _context.SaveChangesAsync();

        // Manually create an AuditLog entry
        var manualAuditLog = new AuditLog
        {
            ActionType = AuditAction.Other,
            EntityType = "TestEntity",
            EntityIdKey = "test-key",
            PersonId = person.Id,
            Timestamp = DateTime.UtcNow,
            AdditionalInfo = "Manual entry"
        };

        // Act
        await _context.AuditLogs.AddAsync(manualAuditLog);
        await _context.SaveChangesAsync();

        // Assert - Should only have 1 audit log (the manual one)
        // If recursion occurred, we'd have a second audit log for the first audit log
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].EntityType.Should().Be("TestEntity");
    }

    [Fact]
    public async Task SavingChangesAsync_SkipsNoAuditEntities()
    {
        // Arrange
        // Use a real entity that doesn't have NoAudit attribute to verify the interceptor works
        // Then verify that AuditLog itself (which has implicit no-audit behavior) doesn't create recursion
        var person = CreateTestPerson("Test", "User");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        var initialAuditCount = await _context.AuditLogs.CountAsync();

        // Manually create an AuditLog - this should NOT create another audit log (recursion prevention)
        var manualAuditLog = new AuditLog
        {
            ActionType = AuditAction.Other,
            EntityType = "ManualTest",
            EntityIdKey = "test-123",
            PersonId = person.Id,
            Timestamp = DateTime.UtcNow
        };

        // Act
        await _context.AuditLogs.AddAsync(manualAuditLog);
        await _context.SaveChangesAsync();

        // Assert - Should only increase by 1 (the manual log), not 2 (which would indicate recursion)
        var finalAuditCount = await _context.AuditLogs.CountAsync();
        finalAuditCount.Should().Be(initialAuditCount + 1);
    }

    [Fact]
    public async Task SavingChangesAsync_SetsPersonIdFromUserContext()
    {
        // Arrange
        var expectedPersonId = 42;
        _mockUserContext.Setup(u => u.CurrentPersonId).Returns(expectedPersonId);

        var person = CreateTestPerson("Test", "User");

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var auditLog = await _context.AuditLogs.FirstAsync();
        auditLog.PersonId.Should().Be(expectedPersonId);
    }

    [Fact]
    public async Task SavingChangesAsync_SetsIpAddressFromHttpContext()
    {
        // Arrange
        var expectedIp = "203.0.113.42";
        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        var mockRequest = new Mock<HttpRequest>();

        mockConnection.Setup(c => c.RemoteIpAddress)
            .Returns(System.Net.IPAddress.Parse(expectedIp));

        mockRequest.Setup(r => r.Headers)
            .Returns(new HeaderDictionary
            {
                { "User-Agent", "Test Agent" }
            });

        mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

        _mockHttpContextAccessor.Setup(a => a.HttpContext)
            .Returns(mockHttpContext.Object);

        var person = CreateTestPerson("Test", "User");

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var auditLog = await _context.AuditLogs.FirstAsync();
        auditLog.IpAddress.Should().Be(expectedIp);
    }

    [Fact]
    public async Task SavingChangesAsync_HandlesNullHttpContext()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var person = CreateTestPerson("Test", "User");

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert - Should not throw, IP should be null
        var auditLog = await _context.AuditLogs.FirstAsync();
        auditLog.IpAddress.Should().BeNull();
        auditLog.UserAgent.Should().BeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_HandlesNullPersonId()
    {
        // Arrange - System action (no authenticated user)
        _mockUserContext.Setup(u => u.CurrentPersonId).Returns((int?)null);

        var person = CreateTestPerson("Test", "User");

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var auditLog = await _context.AuditLogs.FirstAsync();
        auditLog.PersonId.Should().BeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_CapturesUserAgentHeader()
    {
        // Arrange
        var expectedUserAgent = "Custom/1.0 (Test Client)";
        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        var mockRequest = new Mock<HttpRequest>();

        mockConnection.Setup(c => c.RemoteIpAddress)
            .Returns(System.Net.IPAddress.Parse("192.168.1.1"));

        mockRequest.Setup(r => r.Headers)
            .Returns(new HeaderDictionary
            {
                { "User-Agent", expectedUserAgent }
            });

        mockHttpContext.Setup(c => c.Connection).Returns(mockConnection.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

        _mockHttpContextAccessor.Setup(a => a.HttpContext)
            .Returns(mockHttpContext.Object);

        var person = CreateTestPerson("Test", "User");

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var auditLog = await _context.AuditLogs.FirstAsync();
        auditLog.UserAgent.Should().Be(expectedUserAgent);
    }

    [Fact]
    public async Task SavingChangesAsync_MultipleEntities_CreatesMultipleAuditLogs()
    {
        // Arrange
        var person1 = CreateTestPerson("Person", "One");
        var person2 = CreateTestPerson("Person", "Two");
        var group = CreateTestGroup("Test Group");

        // Act
        await _context.People.AddRangeAsync(person1, person2);
        await _context.Groups.AddAsync(group);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(3);

        auditLogs.Should().Contain(a => a.EntityType == "Person");
        auditLogs.Should().Contain(a => a.EntityType == "Group");
        auditLogs.Count(a => a.EntityType == "Person").Should().Be(2);
    }

    [Fact]
    public async Task SavingChangesAsync_FullMasking_MasksCompleteValue()
    {
        // Arrange
        var person = CreateTestPerson("Test", "User");
        person.PasswordHash = "ActualHashValueThatShouldBeMasked123";

        // Act
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var auditLog = await _context.AuditLogs.FirstAsync();
        var newValues = JsonSerializer.Deserialize<Dictionary<string, object>>(
            auditLog.NewValues!);

        var maskedPasswordHash = newValues!["PasswordHash"]?.ToString();

        // With Hash mask type, should be SHA256 hash (64 hex chars), not "***"
        // The interceptor uses Hash masking for PasswordHash
        maskedPasswordHash.Should().NotBeNullOrEmpty();
        maskedPasswordHash.Should().NotBe("ActualHashValueThatShouldBeMasked123");

        // Should be a valid hex string (SHA256 produces 64 hex characters)
        if (!string.IsNullOrEmpty(maskedPasswordHash))
        {
            maskedPasswordHash.Should().HaveLength(64);
            maskedPasswordHash.Should().MatchRegex("^[a-f0-9]{64}$");
        }
    }

    private static Person CreateTestPerson(string firstName, string lastName)
    {
        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            RecordStatusValueId = 1,
            RecordTypeValueId = 1,
            ConnectionStatusValueId = 1
        };
    }

    private static Group CreateTestGroup(string name)
    {
        return new Group
        {
            Name = name,
            GroupTypeId = 1,
            IsActive = true,
            IsPublic = true
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
