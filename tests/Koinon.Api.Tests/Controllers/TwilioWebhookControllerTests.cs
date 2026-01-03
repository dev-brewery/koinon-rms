using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs.Communications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class TwilioWebhookControllerTests
{
    private readonly Mock<ILogger<TwilioWebhookController>> _loggerMock;
    private readonly TwilioWebhookController _controller;

    public TwilioWebhookControllerTests()
    {
        _loggerMock = new Mock<ILogger<TwilioWebhookController>>();
        _controller = new TwilioWebhookController(_loggerMock.Object);
    }

    #region ReceiveStatusCallback Tests

    [Fact]
    public void ReceiveStatusCallback_WithValidPayload_ReturnsNoContent()
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
        var result = _controller.ReceiveStatusCallback(payload);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ReceiveStatusCallback_WithDeliveredStatus_LogsInformation()
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
        _controller.ReceiveStatusCallback(payload);

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
    public void ReceiveStatusCallback_WithFailedStatus_LogsWarning()
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
        _controller.ReceiveStatusCallback(payload);

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
    public void ReceiveStatusCallback_WithUndeliveredStatus_LogsWarning()
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
        _controller.ReceiveStatusCallback(payload);

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
    public void ReceiveStatusCallback_WithQueuedStatus_LogsInformationOnly()
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
        _controller.ReceiveStatusCallback(payload);

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
    public void ReceiveStatusCallback_WithMinimalPayload_ReturnsNoContent()
    {
        // Arrange - Twilio might send minimal data in some cases
        var payload = new TwilioWebhookDto
        {
            MessageSid = "SM1234567890abcdef1234567890abcdef",
            MessageStatus = "sent"
        };

        // Act
        var result = _controller.ReceiveStatusCallback(payload);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void ReceiveStatusCallback_WithAllStatuses_HandlesCorrectly()
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
            var result = _controller.ReceiveStatusCallback(payload);

            // Assert
            result.Should().BeOfType<NoContentResult>($"status '{status}' should return NoContent");
        }
    }

    #endregion
}
