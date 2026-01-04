using FluentAssertions;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for SmsDeliveryStatusService.
/// Validates the correlation between sent SMS messages and Twilio webhook callbacks.
/// </summary>
public class SmsDeliveryStatusServiceTests
{
    private readonly Mock<ILogger<SmsDeliveryStatusService>> _mockLogger;

    public SmsDeliveryStatusServiceTests()
    {
        _mockLogger = new Mock<ILogger<SmsDeliveryStatusService>>();
    }

    private KoinonDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new KoinonDbContext(options);
    }

    #region SetExternalMessageIdAsync Tests

    [Fact]
    public async Task SetExternalMessageIdAsync_WithValidRecipient_ShouldSetExternalMessageId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        var externalMessageId = "SM1234567890abcdef";

        // Act
        await service.SetExternalMessageIdAsync(recipient.Id, externalMessageId);

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated.Should().NotBeNull();
        updated!.ExternalMessageId.Should().Be(externalMessageId);
        updated.ModifiedDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SetExternalMessageIdAsync_WithNonExistentRecipient_ShouldLogWarningAndReturn()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var nonExistentId = 9999;
        var externalMessageId = "SM1234567890abcdef";

        // Act
        await service.SetExternalMessageIdAsync(nonExistentId, externalMessageId);

        // Assert - Should not throw
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region UpdateDeliveryStatusAsync - Status Mapping Tests

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithQueuedStatus_ShouldUpdateToPending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "queued",
            null,
            null);

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Pending);
        updated.ModifiedDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithSentStatus_ShouldUpdateToPending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "sent",
            null,
            null);

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Pending);
        updated.ModifiedDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithDeliveredStatus_ShouldUpdateToDeliveredAndSetTimestamp()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "delivered",
            null,
            null);

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Delivered);
        updated.DeliveredDateTime.Should().NotBeNull();
        updated.DeliveredDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        updated.ModifiedDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithFailedStatus_ShouldUpdateToFailedAndStoreError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "failed",
            30001,
            "Queue overflow");

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updated.ErrorCode.Should().Be(30001);
        updated.ErrorMessage.Should().Be("Queue overflow");
        updated.ModifiedDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithUndeliveredStatus_ShouldUpdateToFailedAndStoreError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "undelivered",
            30003,
            "Unreachable destination handset");

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updated.ErrorCode.Should().Be(30003);
        updated.ErrorMessage.Should().Be("Unreachable destination handset");
        updated.ModifiedDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    #endregion

    #region UpdateDeliveryStatusAsync - Edge Cases

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithNonExistentMessageId_ShouldLogWarningAndReturn()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var nonExistentMessageId = "SM9999999999999999";

        // Act
        await service.UpdateDeliveryStatusAsync(
            nonExistentMessageId,
            "delivered",
            null,
            null);

        // Assert - Should not throw
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithNullErrorCode_ShouldNotSetErrorCode()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "failed",
            null,
            "Some error occurred");

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updated.ErrorCode.Should().BeNull();
        updated.ErrorMessage.Should().Be("Some error occurred");
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithNullErrorMessage_ShouldNotSetErrorMessage()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "failed",
            30001,
            null);

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updated.ErrorCode.Should().Be(30001);
        updated.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithEmptyErrorMessage_ShouldNotSetErrorMessage()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "failed",
            30001,
            "");

        // Assert
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Failed);
        updated.ErrorCode.Should().Be(30001);
        updated.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_DeliveredTwice_ShouldNotUpdateTimestampSecondTime()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act - First delivery
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "delivered",
            null,
            null);

        var firstUpdate = await context.CommunicationRecipients.FindAsync(recipient.Id);
        var firstDeliveredTime = firstUpdate!.DeliveredDateTime;

        // Wait a bit
        await Task.Delay(100);

        // Act - Second delivery (duplicate webhook)
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "delivered",
            null,
            null);

        // Assert - DeliveredDateTime should remain the same
        var secondUpdate = await context.CommunicationRecipients.FindAsync(recipient.Id);
        secondUpdate!.DeliveredDateTime.Should().Be(firstDeliveredTime);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_WithUnknownStatus_ShouldDefaultToPending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act - Use an unknown status
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "unknown_status",
            null,
            null);

        // Assert - Should default to Pending
        var updated = await context.CommunicationRecipients.FindAsync(recipient.Id);
        updated!.Status.Should().Be(CommunicationRecipientStatus.Pending);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task SetExternalMessageIdAsync_ShouldLogDebugMessage()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        var externalMessageId = "SM1234567890abcdef";

        // Act
        await service.SetExternalMessageIdAsync(recipient.Id, externalMessageId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(externalMessageId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_ShouldLogInformationMessage()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new SmsDeliveryStatusService(context, _mockLogger.Object);

        var recipient = new CommunicationRecipient
        {
            CommunicationId = 1,
            PersonId = 1,
            Address = "+15551234567",
            Status = CommunicationRecipientStatus.Pending,
            ExternalMessageId = "SM1234567890abcdef"
        };
        context.CommunicationRecipients.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDeliveryStatusAsync(
            recipient.ExternalMessageId!,
            "delivered",
            null,
            null);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated delivery status")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
