using FluentAssertions;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for ParentPagingService.
/// </summary>
public class ParentPagingServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ISmsService> _mockSmsService;
    private readonly Mock<ILogger<ParentPagingService>> _mockLogger;
    private readonly ParentPagingService _service;

    public ParentPagingServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Setup mocks
        _mockSmsService = new Mock<ISmsService>();
        _mockLogger = new Mock<ILogger<ParentPagingService>>();

        // Default SMS service configuration
        _mockSmsService.Setup(s => s.IsConfigured).Returns(true);

        // Create service
        _service = new ParentPagingService(_context, _mockSmsService.Object, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create family group type for roles
        var familyGroupType = new GroupType
        {
            Id = 1,
            Name = "Family",
            Guid = SystemGuid.GroupType.Family,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.GroupTypes.Add(familyGroupType);

        // Create adult and child roles
        var adultRole = new GroupTypeRole
        {
            Id = 1,
            GroupTypeId = 1,
            Name = "Adult",
            Guid = SystemGuid.GroupTypeRole.FamilyAdult,
            CreatedDateTime = DateTime.UtcNow
        };

        var childRole = new GroupTypeRole
        {
            Id = 2,
            GroupTypeId = 1,
            Name = "Child",
            Guid = SystemGuid.GroupTypeRole.FamilyChild,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Set<GroupTypeRole>().AddRange(adultRole, childRole);

        // Create family using Family entity
        var family = new Family
        {
            Id = 1,
            Name = "Doe Family",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Families.Add(family);

        // Create parent
        var parent = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            CreatedDateTime = DateTime.UtcNow
        };

        // Add mobile phone to parent
        var parentPhone = new PhoneNumber
        {
            Id = 1,
            PersonId = 1,
            Number = "+15551234567",
            NumberNormalized = "15551234567",
            IsMessagingEnabled = true,
            CreatedDateTime = DateTime.UtcNow
        };
        parent.PhoneNumbers.Add(parentPhone);

        // Create child
        var child = new Person
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Doe",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.People.AddRange(parent, child);

        // Add family members
        var parentMember = new FamilyMember
        {
            Id = 1,
            FamilyId = 1,
            PersonId = 1,
            FamilyRoleId = 1,
            IsPrimary = true,
            DateAdded = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        var childMember = new FamilyMember
        {
            Id = 2,
            FamilyId = 1,
            PersonId = 2,
            FamilyRoleId = 2,
            IsPrimary = false,
            DateAdded = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.FamilyMembers.AddRange(parentMember, childMember);

        // Create person aliases
        var parentAlias = new PersonAlias
        {
            Id = 1,
            PersonId = 1,
            CreatedDateTime = DateTime.UtcNow
        };

        var childAlias = new PersonAlias
        {
            Id = 2,
            PersonId = 2,
            CreatedDateTime = DateTime.UtcNow
        };

        _context.PersonAliases.AddRange(parentAlias, childAlias);

        // Create location
        var location = new Location
        {
            Id = 1,
            Name = "Nursery Room 1",
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Locations.Add(location);

        // Create group for check-in
        var checkInGroup = new Group
        {
            Id = 2,
            Name = "Nursery",
            GroupTypeId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Groups.Add(checkInGroup);

        // Create schedule
        var schedule = new Schedule
        {
            Id = 1,
            Name = "Sunday Morning",
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Schedules.Add(schedule);

        // Create attendance occurrence (fixed with SundayDate)
        var today = DateTime.UtcNow;
        var occurrence = new AttendanceOccurrence
        {
            Id = 1,
            GroupId = 2,
            LocationId = 1,
            ScheduleId = 1,
            OccurrenceDate = DateOnly.FromDateTime(today),
            SundayDate = DateOnly.FromDateTime(today.AddDays(-(int)today.DayOfWeek)),
            CreatedDateTime = DateTime.UtcNow
        };
        _context.AttendanceOccurrences.Add(occurrence);

        // Create attendance for child
        var attendance = new Attendance
        {
            Id = 1,
            OccurrenceId = 1,
            PersonAliasId = 2,
            StartDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.Attendances.Add(attendance);

        // Create staff member for sending pages
        var staff = new Person
        {
            Id = 3,
            FirstName = "Staff",
            LastName = "Member",
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(staff);

        _context.SaveChanges();
    }

    [Fact]
    public async Task AssignPagerAsync_ShouldCreatePagerAssignment()
    {
        // Act
        var result = await _service.AssignPagerAsync(1, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PagerNumber.Should().Be(100); // First pager starts at 100
        result.ChildName.Should().Be("Jane Doe");
        result.GroupName.Should().Be("Nursery");
        result.LocationName.Should().Be("Nursery Room 1");
        result.ParentPhoneNumber.Should().Be("+15551234567");
        result.MessagesSentCount.Should().Be(0);

        // Verify database record
        var pagerAssignment = await _context.PagerAssignments.FirstOrDefaultAsync();
        pagerAssignment.Should().NotBeNull();
        pagerAssignment!.PagerNumber.Should().Be(100);
        pagerAssignment.AttendanceId.Should().Be(1);
    }

    [Fact]
    public async Task GetNextPagerNumberAsync_ShouldStartAt100()
    {
        // Act
        var pagerNumber = await _service.GetNextPagerNumberAsync(null, DateTime.UtcNow, CancellationToken.None);

        // Assert
        pagerNumber.Should().Be(100);
    }

    [Fact]
    public async Task GetNextPagerNumberAsync_ShouldIncrement()
    {
        // Arrange
        var pagerAssignment = new PagerAssignment
        {
            Id = 1,
            AttendanceId = 1,
            PagerNumber = 100,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.PagerAssignments.Add(pagerAssignment);
        await _context.SaveChangesAsync();

        // Act
        var pagerNumber = await _service.GetNextPagerNumberAsync(null, DateTime.UtcNow, CancellationToken.None);

        // Assert
        pagerNumber.Should().Be(101);
    }

    [Fact]
    public async Task GetNextPagerNumberAsync_ShouldResetDaily()
    {
        // Arrange - Create pager from yesterday
        var pagerAssignment = new PagerAssignment
        {
            Id = 1,
            AttendanceId = 1,
            PagerNumber = 150,
            CreatedDateTime = DateTime.UtcNow.AddDays(-1)
        };
        _context.PagerAssignments.Add(pagerAssignment);
        await _context.SaveChangesAsync();

        // Act - Get pager for today
        var pagerNumber = await _service.GetNextPagerNumberAsync(null, DateTime.UtcNow, CancellationToken.None);

        // Assert - Should reset to 100
        pagerNumber.Should().Be(100);
    }

    [Fact]
    public async Task SendPageAsync_ShouldSendSuccessfully()
    {
        // Arrange
        var pagerAssignment = await CreatePagerAssignmentAsync();

        _mockSmsService
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(true, "MSG123", null));

        var request = new SendPageRequest("100", PagerMessageType.PickupNeeded, null);

        // Act
        var result = await _service.SendPageAsync(request, 3, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.MessageType.Should().Be(PagerMessageType.PickupNeeded);
        result.Value.Status.Should().Be(PagerMessageStatus.Sent);
        result.Value.MessageText.Should().Contain("Jane Doe");
        result.Value.MessageText.Should().Contain("Nursery Room 1");

        // Verify SMS was sent
        _mockSmsService.Verify(s => s.SendSmsAsync(
            "+15551234567",
            It.Is<string>(msg => msg.Contains("Jane Doe") && msg.Contains("Nursery Room 1")),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify database record
        var message = await _context.PagerMessages.FirstOrDefaultAsync();
        message.Should().NotBeNull();
        message!.TwilioMessageSid.Should().Be("MSG123");
    }

    [Fact]
    public async Task SendPageAsync_WithPagerPrefix_ShouldHandlePagerFormat()
    {
        // Arrange
        await CreatePagerAssignmentAsync();

        _mockSmsService
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(true, "MSG123", null));

        var request = new SendPageRequest("P-100", PagerMessageType.PickupNeeded, null);

        // Act
        var result = await _service.SendPageAsync(request, 3, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendPageAsync_WithCustomMessage_ShouldUseCustomText()
    {
        // Arrange
        await CreatePagerAssignmentAsync();

        _mockSmsService
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(true, "MSG123", null));

        var customMessage = "Please come to the front desk.";
        var request = new SendPageRequest("100", PagerMessageType.Custom, customMessage);

        // Act
        var result = await _service.SendPageAsync(request, 3, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MessageText.Should().Be(customMessage);

        _mockSmsService.Verify(s => s.SendSmsAsync(
            It.IsAny<string>(),
            customMessage,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendPageAsync_CustomMessageWithoutText_ShouldReturnError()
    {
        // Arrange
        await CreatePagerAssignmentAsync();

        var request = new SendPageRequest("100", PagerMessageType.Custom, null);

        // Act
        var result = await _service.SendPageAsync(request, 3, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("UNPROCESSABLE_ENTITY");
        result.Error.Message.Should().Contain("Custom message text is required");
    }

    [Fact]
    public async Task SendPageAsync_RateLimitExceeded_ShouldReturnError()
    {
        // Arrange
        var pagerAssignment = await CreatePagerAssignmentAsync();

        // Create 3 messages in the last hour
        for (int i = 0; i < 3; i++)
        {
            var message = new PagerMessage
            {
                Id = i + 1,
                PagerAssignmentId = pagerAssignment.Id,
                SentByPersonId = 3,
                MessageType = PagerMessageType.PickupNeeded,
                MessageText = "Test message",
                PhoneNumber = "+15551234567",
                Status = PagerMessageStatus.Sent,
                SentDateTime = DateTime.UtcNow.AddMinutes(-30),
                CreatedDateTime = DateTime.UtcNow.AddMinutes(-30)
            };
            _context.PagerMessages.Add(message);
        }
        await _context.SaveChangesAsync();

        var request = new SendPageRequest("100", PagerMessageType.PickupNeeded, null);

        // Act
        var result = await _service.SendPageAsync(request, 3, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("UNPROCESSABLE_ENTITY");
        result.Error.Message.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task SendPageAsync_PagerNotFound_ShouldReturnError()
    {
        // Arrange
        var request = new SendPageRequest("999", PagerMessageType.PickupNeeded, null);

        // Act
        var result = await _service.SendPageAsync(request, 3, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task SendPageAsync_SmsServiceNotConfigured_ShouldRecordFailure()
    {
        // Arrange
        await CreatePagerAssignmentAsync();

        _mockSmsService.Setup(s => s.IsConfigured).Returns(false);

        var request = new SendPageRequest("100", PagerMessageType.PickupNeeded, null);

        // Act
        var result = await _service.SendPageAsync(request, 3, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PagerMessageStatus.Failed);

        var message = await _context.PagerMessages.FirstOrDefaultAsync();
        message.Should().NotBeNull();
        message!.Status.Should().Be(PagerMessageStatus.Failed);
        message.FailureReason.Should().Contain("SMS service is not configured");
    }

    [Fact]
    public async Task SearchPagerAsync_ByPagerNumber_ShouldReturnMatches()
    {
        // Arrange
        await CreatePagerAssignmentAsync();

        var request = new PageSearchRequest("100", null, DateTime.UtcNow);

        // Act
        var results = await _service.SearchPagerAsync(request, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].PagerNumber.Should().Be(100);
        results[0].ChildName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task SearchPagerAsync_ByChildName_ShouldReturnMatches()
    {
        // Arrange
        await CreatePagerAssignmentAsync();

        var request = new PageSearchRequest("Jane", null, DateTime.UtcNow);

        // Act
        var results = await _service.SearchPagerAsync(request, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].ChildName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task GetPageHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        var pagerAssignment = await CreatePagerAssignmentAsync();

        // Create a message
        var message = new PagerMessage
        {
            Id = 1,
            PagerAssignmentId = pagerAssignment.Id,
            SentByPersonId = 3,
            MessageType = PagerMessageType.PickupNeeded,
            MessageText = "Test message",
            PhoneNumber = "+15551234567",
            Status = PagerMessageStatus.Sent,
            SentDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.PagerMessages.Add(message);
        await _context.SaveChangesAsync();

        // Act
        var history = await _service.GetPageHistoryAsync(100, DateTime.UtcNow, CancellationToken.None);

        // Assert
        history.Should().NotBeNull();
        history!.PagerNumber.Should().Be(100);
        history.ChildName.Should().Be("Jane Doe");
        history.Messages.Should().HaveCount(1);
        history.Messages[0].MessageType.Should().Be(PagerMessageType.PickupNeeded);
    }

    [Fact]
    public async Task GetPageHistoryAsync_NotFound_ShouldReturnNull()
    {
        // Act
        var history = await _service.GetPageHistoryAsync(999, DateTime.UtcNow, CancellationToken.None);

        // Assert
        history.Should().BeNull();
    }

    private async Task<PagerAssignment> CreatePagerAssignmentAsync()
    {
        var pagerAssignment = new PagerAssignment
        {
            Id = 1,
            AttendanceId = 1,
            PagerNumber = 100,
            LocationId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.PagerAssignments.Add(pagerAssignment);
        await _context.SaveChangesAsync();
        return pagerAssignment;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
