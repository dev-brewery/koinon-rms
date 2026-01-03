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

/// <summary>
/// Tests for CommunicationSender service.
/// </summary>
public class CommunicationSenderTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ISmsService> _mockSmsService;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly Mock<IMergeFieldService> _mockMergeFieldService;
    private readonly Mock<ILogger<CommunicationSender>> _mockLogger;
    private readonly CommunicationSender _service;

    public CommunicationSenderTests()
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
        _mockEmailSender = new Mock<IEmailSender>();
        _mockMergeFieldService = new Mock<IMergeFieldService>();
        _mockLogger = new Mock<ILogger<CommunicationSender>>();

        // Setup merge field service to pass through by default
        _mockMergeFieldService
            .Setup(m => m.ReplaceMergeFields(It.IsAny<string>(), It.IsAny<Person>()))
            .Returns<string, Person>((template, person) => template);

        // Create service
        _service = new CommunicationSender(
            _context,
            _mockSmsService.Object,
            _mockEmailSender.Object,
            _mockMergeFieldService.Object,
            _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create people
        var person1 = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        var person2 = new Person
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.People.AddRange(person1, person2);

        // Add phone number for SMS tests
        var phone = new PhoneNumber
        {
            Id = 1,
            PersonId = 1,
            Number = "+15551234567",
            NumberTypeValueId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.PhoneNumbers.Add(phone);

        _context.SaveChanges();
    }

    [Fact]
    public async Task SendCommunicationAsync_WithPendingSmsCommuncation_SendsSmsToAllRecipients()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 1,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Pending,
            Body = "Test SMS message",
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient1 = new CommunicationRecipient
        {
            Id = 1,
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            RecipientName = "John Doe",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient2 = new CommunicationRecipient
        {
            Id = 2,
            CommunicationId = 1,
            PersonId = 2,
            Address = "+15559876543",
            RecipientName = "Jane Smith",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        communication.Recipients = new List<CommunicationRecipient> { recipient1, recipient2 };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Mock successful SMS sends
        _mockSmsService
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(Success: true, MessageId: "test-sid", ErrorMessage: null));

        // Act
        await _service.SendCommunicationAsync(1);

        // Assert
        var sentCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == 1);

        sentCommunication.Status.Should().Be(CommunicationStatus.Sent);
        sentCommunication.SentDateTime.Should().NotBeNull();
        sentCommunication.DeliveredCount.Should().Be(2);
        sentCommunication.FailedCount.Should().Be(0);

        sentCommunication.Recipients.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(CommunicationRecipientStatus.Delivered);
            r.DeliveredDateTime.Should().NotBeNull();
            r.ErrorMessage.Should().BeNull();
        });

        _mockSmsService.Verify(
            s => s.SendSmsAsync("+15551234567", "Test SMS message", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockSmsService.Verify(
            s => s.SendSmsAsync("+15559876543", "Test SMS message", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCommunicationAsync_WithPendingEmailCommunication_SendsEmailToAllRecipients()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 2,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Pending,
            Subject = "Test Subject",
            Body = "<p>Test email body</p>",
            FromEmail = "noreply@example.com",
            FromName = "Test Sender",
            ReplyToEmail = "reply@example.com",
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient = new CommunicationRecipient
        {
            Id = 3,
            CommunicationId = 2,
            PersonId = 1,
            Address = "john@example.com",
            RecipientName = "John Doe",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        communication.Recipients = new List<CommunicationRecipient> { recipient };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Mock successful email send
        _mockEmailSender
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendCommunicationAsync(2);

        // Assert
        var sentCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == 2);

        sentCommunication.Status.Should().Be(CommunicationStatus.Sent);
        sentCommunication.DeliveredCount.Should().Be(1);
        sentCommunication.FailedCount.Should().Be(0);

        var sentRecipient = sentCommunication.Recipients.First();
        sentRecipient.Status.Should().Be(CommunicationRecipientStatus.Delivered);
        sentRecipient.DeliveredDateTime.Should().NotBeNull();

        _mockEmailSender.Verify(
            e => e.SendEmailAsync(
                "john@example.com",
                "John Doe",
                "noreply@example.com",
                "Test Sender",
                "Test Subject",
                "<p>Test email body</p>",
                "reply@example.com",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCommunicationAsync_WithSmsFailure_MarksRecipientAsFailed()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 3,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Pending,
            Body = "Test message",
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient = new CommunicationRecipient
        {
            Id = 4,
            CommunicationId = 3,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        communication.Recipients = new List<CommunicationRecipient> { recipient };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Mock failed SMS send
        _mockSmsService
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(Success: false, MessageId: null, ErrorMessage: "Invalid phone number"));

        // Act
        await _service.SendCommunicationAsync(3);

        // Assert
        var sentCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == 3);

        sentCommunication.Status.Should().Be(CommunicationStatus.Failed);
        sentCommunication.DeliveredCount.Should().Be(0);
        sentCommunication.FailedCount.Should().Be(1);

        var failedRecipient = sentCommunication.Recipients.First();
        failedRecipient.Status.Should().Be(CommunicationRecipientStatus.Failed);
        failedRecipient.DeliveredDateTime.Should().BeNull();
        failedRecipient.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task SendCommunicationAsync_WithMixedSuccessAndFailure_UpdatesCountsCorrectly()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 4,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Pending,
            Body = "Test message",
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient1 = new CommunicationRecipient
        {
            Id = 5,
            CommunicationId = 4,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient2 = new CommunicationRecipient
        {
            Id = 6,
            CommunicationId = 4,
            PersonId = 2,
            Address = "+15559999999",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        communication.Recipients = new List<CommunicationRecipient> { recipient1, recipient2 };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Mock mixed results
        _mockSmsService
            .Setup(s => s.SendSmsAsync("+15551234567", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(Success: true, MessageId: "sid1", ErrorMessage: null));

        _mockSmsService
            .Setup(s => s.SendSmsAsync("+15559999999", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(Success: false, MessageId: null, ErrorMessage: "Failed"));

        // Act
        await _service.SendCommunicationAsync(4);

        // Assert
        var sentCommunication = await _context.Communications
            .Include(c => c.Recipients)
            .FirstAsync(c => c.Id == 4);

        sentCommunication.Status.Should().Be(CommunicationStatus.Sent);
        sentCommunication.DeliveredCount.Should().Be(1);
        sentCommunication.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task SendCommunicationAsync_WithDraftStatus_SkipsSending()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 5,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Draft,
            Body = "Test message",
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Act
        await _service.SendCommunicationAsync(5);

        // Assert
        var draftCommunication = await _context.Communications.FirstAsync(c => c.Id == 5);
        draftCommunication.Status.Should().Be(CommunicationStatus.Draft);
        draftCommunication.SentDateTime.Should().BeNull();

        _mockSmsService.Verify(
            s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendCommunicationAsync_WithAlreadySentStatus_SkipsSending()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 6,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Subject = "Already sent",
            Body = "Body",
            SentDateTime = DateTime.UtcNow.AddHours(-1),
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Act
        await _service.SendCommunicationAsync(6);

        // Assert
        _mockEmailSender.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendCommunicationAsync_WithNonExistentCommunication_DoesNotThrow()
    {
        // Act
        var act = async () => await _service.SendCommunicationAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendCommunicationAsync_SkipsAlreadyProcessedRecipients()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 7,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Pending,
            Body = "Test message",
            CreatedDateTime = DateTime.UtcNow
        };

        var deliveredRecipient = new CommunicationRecipient
        {
            Id = 7,
            CommunicationId = 7,
            PersonId = 1,
            Address = "+15551111111",
            Status = CommunicationRecipientStatus.Delivered, // Already delivered
            DeliveredDateTime = DateTime.UtcNow.AddMinutes(-5),
            CreatedDateTime = DateTime.UtcNow
        };

        var pendingRecipient = new CommunicationRecipient
        {
            Id = 8,
            CommunicationId = 7,
            PersonId = 2,
            Address = "+15552222222",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        communication.Recipients = new List<CommunicationRecipient> { deliveredRecipient, pendingRecipient };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _mockSmsService
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(Success: true, MessageId: "sid", ErrorMessage: null));

        // Act
        await _service.SendCommunicationAsync(7);

        // Assert
        _mockSmsService.Verify(
            s => s.SendSmsAsync("+15552222222", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Should NOT attempt to send to already-delivered recipient
        _mockSmsService.Verify(
            s => s.SendSmsAsync("+15551111111", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendCommunicationAsync_WithSms_ReplacesMergeFieldsForEachRecipient()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 8,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Pending,
            Body = "Hello {{FirstName}}, this is a test message.",
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient1 = new CommunicationRecipient
        {
            Id = 9,
            CommunicationId = 8,
            PersonId = 1,
            Address = "+15551234567",
            RecipientName = "John Doe",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient2 = new CommunicationRecipient
        {
            Id = 10,
            CommunicationId = 8,
            PersonId = 2,
            Address = "+15559876543",
            RecipientName = "Jane Smith",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        communication.Recipients = new List<CommunicationRecipient> { recipient1, recipient2 };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Setup merge field service to replace fields
        var person1 = await _context.People.FindAsync(1);
        var person2 = await _context.People.FindAsync(2);

        _mockMergeFieldService
            .Setup(m => m.ReplaceMergeFields("Hello {{FirstName}}, this is a test message.", person1!))
            .Returns("Hello John, this is a test message.");

        _mockMergeFieldService
            .Setup(m => m.ReplaceMergeFields("Hello {{FirstName}}, this is a test message.", person2!))
            .Returns("Hello Jane, this is a test message.");

        _mockSmsService
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult(Success: true, MessageId: "test-sid", ErrorMessage: null));

        // Act
        await _service.SendCommunicationAsync(8);

        // Assert
        _mockMergeFieldService.Verify(
            m => m.ReplaceMergeFields("Hello {{FirstName}}, this is a test message.", person1!),
            Times.Once,
            "Should replace merge fields for person 1");

        _mockMergeFieldService.Verify(
            m => m.ReplaceMergeFields("Hello {{FirstName}}, this is a test message.", person2!),
            Times.Once,
            "Should replace merge fields for person 2");

        _mockSmsService.Verify(
            s => s.SendSmsAsync("+15551234567", "Hello John, this is a test message.", It.IsAny<CancellationToken>()),
            Times.Once,
            "Should send personalized SMS to John");

        _mockSmsService.Verify(
            s => s.SendSmsAsync("+15559876543", "Hello Jane, this is a test message.", It.IsAny<CancellationToken>()),
            Times.Once,
            "Should send personalized SMS to Jane");
    }

    [Fact]
    public async Task SendCommunicationAsync_WithEmail_ReplacesMergeFieldsInSubjectAndBody()
    {
        // Arrange
        var communication = new Communication
        {
            Id = 9,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Pending,
            Subject = "Welcome {{FirstName}}!",
            Body = "<p>Hello {{FirstName}} {{LastName}}, welcome to our community.</p>",
            FromEmail = "noreply@example.com",
            FromName = "Test Sender",
            CreatedDateTime = DateTime.UtcNow
        };

        var recipient = new CommunicationRecipient
        {
            Id = 11,
            CommunicationId = 9,
            PersonId = 1,
            Address = "john@example.com",
            RecipientName = "John Doe",
            Status = CommunicationRecipientStatus.Pending,
            CreatedDateTime = DateTime.UtcNow
        };

        communication.Recipients = new List<CommunicationRecipient> { recipient };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        var person = await _context.People.FindAsync(1);

        _mockMergeFieldService
            .Setup(m => m.ReplaceMergeFields("Welcome {{FirstName}}!", person!))
            .Returns("Welcome John!");

        _mockMergeFieldService
            .Setup(m => m.ReplaceMergeFields("<p>Hello {{FirstName}} {{LastName}}, welcome to our community.</p>", person!))
            .Returns("<p>Hello John Doe, welcome to our community.</p>");

        _mockEmailSender
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendCommunicationAsync(9);

        // Assert
        _mockMergeFieldService.Verify(
            m => m.ReplaceMergeFields("Welcome {{FirstName}}!", person!),
            Times.Once,
            "Should replace merge fields in subject");

        _mockMergeFieldService.Verify(
            m => m.ReplaceMergeFields("<p>Hello {{FirstName}} {{LastName}}, welcome to our community.</p>", person!),
            Times.Once,
            "Should replace merge fields in body");

        _mockEmailSender.Verify(
            e => e.SendEmailAsync(
                "john@example.com",
                "John Doe",
                "noreply@example.com",
                "Test Sender",
                "Welcome John!",
                "<p>Hello John Doe, welcome to our community.</p>",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Should send email with personalized subject and body");
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
