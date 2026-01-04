using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs.Communications;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class TwilioWebhookControllerTests
{
    private readonly Mock<ILogger<TwilioWebhookController>> _loggerMock;
    private readonly Mock<ISmsDeliveryStatusService> _smsDeliveryStatusServiceMock;
    private readonly TwilioWebhookController _controller;

    public TwilioWebhookControllerTests()
    {
        _loggerMock = new Mock<ILogger<TwilioWebhookController>>();
        _smsDeliveryStatusServiceMock = new Mock<ISmsDeliveryStatusService>();
        _controller = new TwilioWebhookController(_loggerMock.Object, _smsDeliveryStatusServiceMock.Object);
    }

    #region ReceiveStatusCallback Tests

    [Fact]
    public async Task ReceiveStatusCallback_WithValidPayload_ReturnsNoContent()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "delivered",
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        var result = await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithDeliveredStatus_LogsInformation()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "delivered",
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("delivered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithFailedStatus_LogsWarning()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "failed",
            To = "+15551234567",
            From = "+15559876543",
            ErrorCode = 30001,
            ErrorMessage = "Queue overflow"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithUndeliveredStatus_LogsWarning()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "undelivered",
            To = "+15551234567",
            From = "+15559876543",
            ErrorCode = 30003,
            ErrorMessage = "Unreachable destination handset"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithQueuedStatus_LogsInformationOnly()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "queued",
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Should not log warning for queued status
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithMinimalPayload_ReturnsNoContent()
    {
        // Arrange - Twilio might send minimal data in some cases
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "sent"
        };

        // Act
        var result = await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithAllStatuses_HandlesCorrectly()
    {
        // Arrange - Test all possible status values
        var statuses = new[] { "queued", "sending", "sent", "delivered", "undelivered", "failed" };

        foreach (var status in statuses)
        {
            var payload = new TwilioWebhookDto
            {
                MessageSid = $"SM{status}1234567890abcdef",
                MessageStatus = status,
                To = "+15551234567",
                From = "+15559876543"
            };

            if (status == "failed" || status == "undelivered")
            {
                payload = payload with { ErrorCode = 30001, ErrorMessage = "Test error" };
            }

            // Act
            var result = await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>($"status '{status}' should return NoContent");
        }
    }

    #endregion

    #region Service Integration Tests

    [Fact]
    public async Task ReceiveStatusCallback_WithValidPayload_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "delivered",
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _smsDeliveryStatusServiceMock.Verify(
            x => x.UpdateDeliveryStatusAsync(
                "SM1234567890abcdef1234567890abcdef",
                "delivered",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithErrorDetails_ShouldPassErrorsToService()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "failed",
            To = "+15551234567",
            From = "+15559876543",
            ErrorCode = 30001,
            ErrorMessage = "Queue overflow"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _smsDeliveryStatusServiceMock.Verify(
            x => x.UpdateDeliveryStatusAsync(
                "SM1234567890abcdef1234567890abcdef",
                "failed",
                30001,
                "Queue overflow",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithMissingMessageSid_ShouldNotCallService()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = null,
            MessageStatus = "delivered",
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _smsDeliveryStatusServiceMock.Verify(
            x => x.UpdateDeliveryStatusAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Should log warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing required fields")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithEmptyMessageSid_ShouldNotCallService()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "",
            MessageStatus = "delivered",
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _smsDeliveryStatusServiceMock.Verify(
            x => x.UpdateDeliveryStatusAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Should log warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing required fields")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithMissingMessageStatus_ShouldNotCallService()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = null,
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _smsDeliveryStatusServiceMock.Verify(
            x => x.UpdateDeliveryStatusAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Should log warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing required fields")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithEmptyMessageStatus_ShouldNotCallService()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "",
            To = "+15551234567",
            From = "+15559876543"
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _smsDeliveryStatusServiceMock.Verify(
            x => x.UpdateDeliveryStatusAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Should log warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing required fields")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveStatusCallback_WithNullErrorCodeAndMessage_ShouldPassNullsToService()
    {
        // Arrange
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "delivered",
            To = "+15551234567",
            From = "+15559876543",
            ErrorCode = null,
            ErrorMessage = null
        };

        // Act
        await _controller.ReceiveStatusCallback(payload, CancellationToken.None);

        // Assert
        _smsDeliveryStatusServiceMock.Verify(
            x => x.UpdateDeliveryStatusAsync(
                "SM1234567890abcdef1234567890abcdef",
                "delivered",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
