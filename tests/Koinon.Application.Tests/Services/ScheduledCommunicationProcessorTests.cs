using FluentAssertions;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class ScheduledCommunicationProcessorTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ICommunicationPreferenceService> _mockPreferenceService;
    private readonly Mock<ILogger<ScheduledCommunicationProcessor>> _mockLogger;
    private readonly ScheduledCommunicationProcessor _processor;

    public ScheduledCommunicationProcessorTests()
    {
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();
        _mockPreferenceService = new Mock<ICommunicationPreferenceService>();
        _mockLogger = new Mock<ILogger<ScheduledCommunicationProcessor>>();

        _processor = new ScheduledCommunicationProcessor(
            _context,
            _mockPreferenceService.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_NoScheduledCommunications_ReturnsZero()
    {
        // Arrange
        // Create a communication that is NOT scheduled
        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Body = "Test",
            ScheduledDateTime = null
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_ScheduledNotDue_ReturnsZero()
    {
        // Arrange
        var futureDateTime = DateTime.UtcNow.AddHours(1);
        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test",
            ScheduledDateTime = futureDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_ScheduledDue_NoOptOuts_TransitionsToPending()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var person1 = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var person2 = new Person
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com"
        };

        _context.People.AddRange(person1, person2);
        await _context.SaveChangesAsync();

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test scheduled email",
            Subject = "Test Subject",
            ScheduledDateTime = pastDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        var recipient1 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person1.Id,
            Address = person1.Email!,
            RecipientName = $"{person1.FirstName} {person1.LastName}",
            Status = CommunicationRecipientStatus.Pending
        };
        var recipient2 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person2.Id,
            Address = person2.Email!,
            RecipientName = $"{person2.FirstName} {person2.LastName}",
            Status = CommunicationRecipientStatus.Pending
        };
        _context.CommunicationRecipients.AddRange(recipient1, recipient2);
        await _context.SaveChangesAsync();

        // Mock opt-out check - neither person opted out
        _mockPreferenceService
            .Setup(s => s.IsOptedOutBatchAsync(
                It.IsAny<List<int>>(),
                CommunicationType.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool>
            {
                { person1.Id, false },
                { person2.Id, false }
            });

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(1);

        var updatedCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == communication.Id);

        updatedCommunication.Status.Should().Be(CommunicationStatus.Pending);
        updatedCommunication.Recipients.Should().AllSatisfy(r =>
            r.Status.Should().Be(CommunicationRecipientStatus.Pending));

        _mockPreferenceService.Verify(
            s => s.IsOptedOutBatchAsync(
                It.Is<List<int>>(list => list.Contains(person1.Id) && list.Contains(person2.Id)),
                CommunicationType.Email,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_SomeRecipientsOptedOut_FiltersAndTransitionsToPending()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var person1 = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var person2 = new Person
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com"
        };

        _context.People.AddRange(person1, person2);
        await _context.SaveChangesAsync();

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Scheduled,
            Body = "Test scheduled SMS",
            ScheduledDateTime = pastDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        var recipient1 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person1.Id,
            Address = "+1234567890",
            RecipientName = $"{person1.FirstName} {person1.LastName}",
            Status = CommunicationRecipientStatus.Pending
        };
        var recipient2 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person2.Id,
            Address = "+0987654321",
            RecipientName = $"{person2.FirstName} {person2.LastName}",
            Status = CommunicationRecipientStatus.Pending
        };
        _context.CommunicationRecipients.AddRange(recipient1, recipient2);
        await _context.SaveChangesAsync();

        // Mock opt-out check - person1 opted out, person2 did not
        _mockPreferenceService
            .Setup(s => s.IsOptedOutBatchAsync(
                It.IsAny<List<int>>(),
                CommunicationType.Sms,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool>
            {
                { person1.Id, true },  // Opted out
                { person2.Id, false }  // Not opted out
            });

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(1);

        var updatedCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == communication.Id);

        updatedCommunication.Status.Should().Be(CommunicationStatus.Pending);

        var updatedRecipient1 = updatedCommunication.Recipients.First(r => r.PersonId == person1.Id);
        updatedRecipient1.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updatedRecipient1.ErrorMessage.Should().Be("Recipient opted out");

        var updatedRecipient2 = updatedCommunication.Recipients.First(r => r.PersonId == person2.Id);
        updatedRecipient2.Status.Should().Be(CommunicationRecipientStatus.Pending);
        updatedRecipient2.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_AllRecipientsOptedOut_MarksAsFailed()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test scheduled email",
            Subject = "Test Subject",
            ScheduledDateTime = pastDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        var recipient = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person.Id,
            Address = person.Email!,
            RecipientName = $"{person.FirstName} {person.LastName}",
            Status = CommunicationRecipientStatus.Pending
        };
        _context.CommunicationRecipients.Add(recipient);
        await _context.SaveChangesAsync();

        // Mock opt-out check - person opted out
        _mockPreferenceService
            .Setup(s => s.IsOptedOutBatchAsync(
                It.IsAny<List<int>>(),
                CommunicationType.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool>
            {
                { person.Id, true }
            });

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(1);

        var updatedCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == communication.Id);

        updatedCommunication.Status.Should().Be(CommunicationStatus.Failed);
        updatedCommunication.FailedCount.Should().Be(1);

        var updatedRecipient = updatedCommunication.Recipients.First();
        updatedRecipient.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updatedRecipient.ErrorMessage.Should().Be("Recipient opted out");
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_MultipleScheduledCommunications_ProcessesAll()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var person1 = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var person2 = new Person
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com"
        };

        _context.People.AddRange(person1, person2);
        await _context.SaveChangesAsync();

        var communication1 = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test 1",
            Subject = "Test 1",
            ScheduledDateTime = pastDateTime
        };

        var communication2 = new Communication
        {
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Scheduled,
            Body = "Test 2",
            ScheduledDateTime = pastDateTime
        };

        _context.Communications.AddRange(communication1, communication2);
        await _context.SaveChangesAsync();

        var recipient1 = new CommunicationRecipient
        {
            CommunicationId = communication1.Id,
            PersonId = person1.Id,
            Address = person1.Email!,
            Status = CommunicationRecipientStatus.Pending
        };
        var recipient2 = new CommunicationRecipient
        {
            CommunicationId = communication2.Id,
            PersonId = person2.Id,
            Address = "+1234567890",
            Status = CommunicationRecipientStatus.Pending
        };
        _context.CommunicationRecipients.AddRange(recipient1, recipient2);
        await _context.SaveChangesAsync();

        // Mock opt-out checks - neither person opted out
        _mockPreferenceService
            .Setup(s => s.IsOptedOutBatchAsync(
                It.IsAny<List<int>>(),
                It.IsAny<CommunicationType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<int> personIds, CommunicationType type, CancellationToken ct) =>
                personIds.ToDictionary(id => id, id => false));

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(2);

        var updatedCommunications = await _context.Communications
            .Include(c => c.Recipients)
            .Where(c => c.Id == communication1.Id || c.Id == communication2.Id)
            .ToListAsync();

        updatedCommunications.Should().AllSatisfy(c =>
            c.Status.Should().Be(CommunicationStatus.Pending));
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_NoRecipients_TransitionsToPending()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test email with no recipients",
            Subject = "Test Subject",
            ScheduledDateTime = pastDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(1);

        var updatedCommunication = await _context.Communications
            .FirstAsync(c => c.Id == communication.Id);

        // Should transition to Pending so it can be marked as complete
        updatedCommunication.Status.Should().Be(CommunicationStatus.Pending);
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_AlreadyProcessed_SkipsCommunication()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test",
            Subject = "Test",
            ScheduledDateTime = pastDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        var recipient = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person.Id,
            Address = person.Email!,
            Status = CommunicationRecipientStatus.Pending
        };
        _context.CommunicationRecipients.Add(recipient);
        await _context.SaveChangesAsync();

        // Manually change status to Pending (simulating another process)
        communication.Status = CommunicationStatus.Pending;
        await _context.SaveChangesAsync();

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(0);

        // Should NOT call preference service since communication is already Pending
        _mockPreferenceService.Verify(
            s => s.IsOptedOutBatchAsync(
                It.IsAny<List<int>>(),
                It.IsAny<CommunicationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_DuplicatePersonIds_ChecksOnce()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };

        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test",
            Subject = "Test",
            ScheduledDateTime = pastDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Create communication with multiple recipients for the same person
        var recipient1 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person.Id,
            Address = person.Email!,
            Status = CommunicationRecipientStatus.Pending
        };
        var recipient2 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person.Id,
            Address = "alternate@example.com",
            Status = CommunicationRecipientStatus.Pending
        };
        _context.CommunicationRecipients.AddRange(recipient1, recipient2);
        await _context.SaveChangesAsync();

        // Mock opt-out check
        _mockPreferenceService
            .Setup(s => s.IsOptedOutBatchAsync(
                It.IsAny<List<int>>(),
                CommunicationType.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool>
            {
                { person.Id, false }
            });

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(1);

        // Verify that preference service was called with a list containing the person ID only once
        _mockPreferenceService.Verify(
            s => s.IsOptedOutBatchAsync(
                It.Is<List<int>>(list => list.Count == 1 && list.Contains(person.Id)),
                CommunicationType.Email,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessScheduledCommunicationsAsync_SomeRecipientsAlreadyFailed_OnlyChecksRemaining()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddMinutes(-5);
        var person1 = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com"
        };
        var person2 = new Person
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com"
        };

        _context.People.AddRange(person1, person2);
        await _context.SaveChangesAsync();

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Scheduled,
            Body = "Test",
            Subject = "Test",
            ScheduledDateTime = pastDateTime
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        var recipient1 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person1.Id,
            Address = person1.Email!,
            Status = CommunicationRecipientStatus.Failed, // Already failed
            ErrorMessage = "Previous error"
        };
        var recipient2 = new CommunicationRecipient
        {
            CommunicationId = communication.Id,
            PersonId = person2.Id,
            Address = person2.Email!,
            Status = CommunicationRecipientStatus.Pending
        };
        _context.CommunicationRecipients.AddRange(recipient1, recipient2);
        await _context.SaveChangesAsync();

        // Mock opt-out check - only person2 should be checked
        _mockPreferenceService
            .Setup(s => s.IsOptedOutBatchAsync(
                It.IsAny<List<int>>(),
                CommunicationType.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool>
            {
                { person2.Id, false }
            });

        // Act
        var result = await _processor.ProcessScheduledCommunicationsAsync();

        // Assert
        result.Should().Be(1);

        // Verify only person2 was checked (not person1 since they were already Failed)
        _mockPreferenceService.Verify(
            s => s.IsOptedOutBatchAsync(
                It.Is<List<int>>(list => list.Count == 1 && list.Contains(person2.Id) && !list.Contains(person1.Id)),
                CommunicationType.Email,
                It.IsAny<CancellationToken>()),
            Times.Once);

        var updatedCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == communication.Id);

        var updatedRecipient1 = updatedCommunication.Recipients.First(r => r.PersonId == person1.Id);
        updatedRecipient1.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updatedRecipient1.ErrorMessage.Should().Be("Previous error"); // Unchanged
    }
}
