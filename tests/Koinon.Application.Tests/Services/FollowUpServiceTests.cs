using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for FollowUpService.
/// </summary>
public class FollowUpServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ILogger<FollowUpService>> _mockLogger;
    private readonly FollowUpService _service;

    public FollowUpServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Setup logger mock
        _mockLogger = new Mock<ILogger<FollowUpService>>();

        // Create service
        _service = new FollowUpService(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add people
        var person1 = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        var person2 = new Person
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        var staffMember = new Person
        {
            Id = 3,
            FirstName = "Staff",
            LastName = "Member",
            Email = "staff@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.People.AddRange(person1, person2, staffMember);

        // Add follow-ups
        var followUp1 = new FollowUp
        {
            Id = 1,
            PersonId = 1,
            Status = FollowUpStatus.Pending,
            Notes = "Initial note",
            CreatedDateTime = DateTime.UtcNow.AddDays(-2)
        };

        var followUp2 = new FollowUp
        {
            Id = 2,
            PersonId = 2,
            Status = FollowUpStatus.Contacted,
            AssignedToPersonId = 3,
            ContactedDateTime = DateTime.UtcNow.AddDays(-1),
            Notes = "Contacted via phone",
            CreatedDateTime = DateTime.UtcNow.AddDays(-3)
        };

        var followUp3 = new FollowUp
        {
            Id = 3,
            PersonId = 1,
            Status = FollowUpStatus.Connected,
            CompletedDateTime = DateTime.UtcNow.AddDays(-5),
            Notes = "Successfully connected",
            CreatedDateTime = DateTime.UtcNow.AddDays(-7)
        };

        _context.Set<FollowUp>().AddRange(followUp1, followUp2, followUp3);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateFollowUpAsync_ShouldCreateFollowUp()
    {
        // Act
        var result = await _service.CreateFollowUpAsync(1, null);

        // Assert
        result.Should().NotBeNull();
        result.PersonId.Should().Be(1);
        result.Status.Should().Be(FollowUpStatus.Pending);
        result.AttendanceId.Should().BeNull();
    }

    [Fact]
    public async Task CreateFollowUpAsync_WithAttendanceId_ShouldLinkAttendance()
    {
        // Arrange
        var attendance = new Attendance
        {
            Id = 100,
            OccurrenceId = 1,
            PersonAliasId = 1,
            StartDateTime = DateTime.UtcNow,
            IsFirstTime = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateFollowUpAsync(1, 100);

        // Assert
        result.AttendanceId.Should().Be(100);
    }

    [Fact]
    public async Task GetPendingFollowUpsAsync_ShouldReturnOnlyPendingAndContactedFollowUps()
    {
        // Act
        var result = await _service.GetPendingFollowUpsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.Status == FollowUpStatus.Pending);
        result.Should().Contain(f => f.Status == FollowUpStatus.Contacted);
        result.Should().NotContain(f => f.Status == FollowUpStatus.Connected);
    }

    [Fact]
    public async Task GetPendingFollowUpsAsync_FilteredByAssignee_ShouldReturnAssignedFollowUps()
    {
        // Arrange
        var staffIdKey = Koinon.Domain.Data.IdKeyHelper.Encode(3);

        // Act
        var result = await _service.GetPendingFollowUpsAsync(staffIdKey);

        // Assert
        result.Should().HaveCount(1);
        result.First().AssignedToName.Should().Be("Staff Member");
    }

    [Fact]
    public async Task GetByIdKeyAsync_ShouldReturnFollowUp()
    {
        // Arrange
        var idKey = Koinon.Domain.Data.IdKeyHelper.Encode(1);

        // Act
        var result = await _service.GetByIdKeyAsync(idKey);

        // Assert
        result.Should().NotBeNull();
        result!.PersonName.Should().Be("John Doe");
        result.Status.Should().Be(FollowUpStatus.Pending);
    }

    [Fact]
    public async Task GetByIdKeyAsync_WithInvalidIdKey_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetByIdKeyAsync("invalid");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var idKey = Koinon.Domain.Data.IdKeyHelper.Encode(1);

        // Act
        var result = await _service.UpdateStatusAsync(
            idKey,
            FollowUpStatus.Contacted,
            "Made contact via email");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(FollowUpStatus.Contacted);
        result.Value.ContactedDateTime.Should().NotBeNull();
        result.Value.Notes.Should().Contain("Made contact via email");
    }

    [Fact]
    public async Task UpdateStatusAsync_ToConnected_ShouldSetCompletedDateTime()
    {
        // Arrange
        var idKey = Koinon.Domain.Data.IdKeyHelper.Encode(1);

        // Act
        var result = await _service.UpdateStatusAsync(
            idKey,
            FollowUpStatus.Connected,
            "Successfully connected");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(FollowUpStatus.Connected);
        result.Value.CompletedDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidIdKey_ShouldReturnFailure()
    {
        // Act
        var result = await _service.UpdateStatusAsync(
            "invalid",
            FollowUpStatus.Connected);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AssignFollowUpAsync_ShouldAssignToPerson()
    {
        // Arrange
        var followUpIdKey = Koinon.Domain.Data.IdKeyHelper.Encode(1);
        var staffIdKey = Koinon.Domain.Data.IdKeyHelper.Encode(3);

        // Act
        var result = await _service.AssignFollowUpAsync(followUpIdKey, staffIdKey);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify assignment
        var followUp = await _service.GetByIdKeyAsync(followUpIdKey);
        followUp!.AssignedToName.Should().Be("Staff Member");
    }

    [Fact]
    public async Task AssignFollowUpAsync_WithInvalidFollowUpId_ShouldReturnFailure()
    {
        // Arrange
        var staffIdKey = Koinon.Domain.Data.IdKeyHelper.Encode(3);

        // Act
        var result = await _service.AssignFollowUpAsync("invalid", staffIdKey);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AssignFollowUpAsync_WithInvalidPersonId_ShouldReturnFailure()
    {
        // Arrange
        var followUpIdKey = Koinon.Domain.Data.IdKeyHelper.Encode(1);

        // Act
        var result = await _service.AssignFollowUpAsync(followUpIdKey, "invalid");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingFollowUpsAsync_ShouldIncludePersonAndAssigneeDetails()
    {
        // Act
        var result = await _service.GetPendingFollowUpsAsync();

        // Assert
        var contactedFollowUp = result.First(f => f.Status == FollowUpStatus.Contacted);
        contactedFollowUp.PersonName.Should().Be("Jane Smith");
        contactedFollowUp.AssignedToName.Should().Be("Staff Member");
        contactedFollowUp.ContactedDateTime.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
