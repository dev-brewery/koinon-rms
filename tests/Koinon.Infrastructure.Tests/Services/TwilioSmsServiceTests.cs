using FluentAssertions;
using Koinon.Infrastructure.Options;
using Koinon.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Koinon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for TwilioSmsService.
/// These tests focus on configuration validation, input validation, and rate limiting.
/// Actual Twilio API calls cannot be easily tested without mocking static TwilioClient.Init,
/// so integration testing is recommended for end-to-end SMS delivery verification.
/// </summary>
public class TwilioSmsServiceTests
{
    private readonly Mock<ILogger<TwilioSmsService>> _mockLogger;

    public TwilioSmsServiceTests()
    {
        _mockLogger = new Mock<ILogger<TwilioSmsService>>();
    }

    private TwilioSmsService CreateService(TwilioOptions options)
    {
        var mockOptions = new Mock<IOptions<TwilioOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        return new TwilioSmsService(mockOptions.Object, _mockLogger.Object);
    }

    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_WithMissingAccountSid_ShouldReturnFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = null,
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithEmptyAccountSid_ShouldReturnFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithMissingAuthToken_ShouldReturnFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = null,
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithEmptyAuthToken_ShouldReturnFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithMissingFromNumber_ShouldReturnFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = null
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithEmptyFromNumber_ShouldReturnFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = ""
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithWhitespaceFromNumber_ShouldReturnFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "   "
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    #endregion

    #region SendSmsAsync Validation Tests

    [Fact]
    public async Task SendSmsAsync_WithUnconfiguredService_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = null,
            AuthToken = null,
            FromNumber = null
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendSmsAsync("+15551234567", "Test message");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("SMS service not configured");
        result.SegmentCount.Should().Be(0);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMS service not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSmsAsync_WithEmptyPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendSmsAsync("", "Test message");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Phone number is required");
        result.SegmentCount.Should().Be(0);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("phone number is empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSmsAsync_WithNullPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendSmsAsync(null!, "Test message");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Phone number is required");
        result.SegmentCount.Should().Be(0);
    }

    [Fact]
    public async Task SendSmsAsync_WithWhitespacePhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendSmsAsync("   ", "Test message");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Phone number is required");
        result.SegmentCount.Should().Be(0);
    }

    [Fact]
    public async Task SendSmsAsync_WithEmptyMessage_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendSmsAsync("+15551234567", "");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Message body is required");
        result.SegmentCount.Should().Be(0);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("message body is empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSmsAsync_WithNullMessage_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendSmsAsync("+15551234567", null!);

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Message body is required");
        result.SegmentCount.Should().Be(0);
    }

    [Fact]
    public async Task SendSmsAsync_WithWhitespaceMessage_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendSmsAsync("+15551234567", "   ");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Message body is required");
        result.SegmentCount.Should().Be(0);
    }

    #endregion

    #region SendMmsAsync Validation Tests

    [Fact]
    public async Task SendMmsAsync_WithUnconfiguredService_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = null,
            AuthToken = null,
            FromNumber = null
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendMmsAsync(
            "+15551234567",
            "Test MMS",
            new[] { "https://example.com/image.jpg" });

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("SMS service not configured");
        result.SegmentCount.Should().Be(0);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMS service not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMmsAsync_WithEmptyPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendMmsAsync(
            "",
            "Test MMS",
            new[] { "https://example.com/image.jpg" });

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Phone number is required");
        result.SegmentCount.Should().Be(0);
    }

    [Fact]
    public async Task SendMmsAsync_WithNullPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendMmsAsync(
            null!,
            "Test MMS",
            new[] { "https://example.com/image.jpg" });

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("Phone number is required");
        result.SegmentCount.Should().Be(0);
    }

    [Fact]
    public async Task SendMmsAsync_WithEmptyMediaUrls_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendMmsAsync(
            "+15551234567",
            "Test MMS",
            Array.Empty<string>());

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("At least one media URL is required for MMS");
        result.SegmentCount.Should().Be(0);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("no media URLs provided")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMmsAsync_WithNullMediaUrls_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var result = await service.SendMmsAsync(
            "+15551234567",
            "Test MMS",
            null!);

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Be("At least one media URL is required for MMS");
        result.SegmentCount.Should().Be(0);
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task SendSmsAsync_ExceedingRateLimit_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act - Attempt to send 11 messages (max is 10 per minute)
        var results = new List<bool>();
        for (int i = 0; i < 11; i++)
        {
            var result = await service.SendSmsAsync("+15559999999", $"Message {i}");
            results.Add(result.Success);
        }

        // Assert - First 10 should fail due to missing Twilio API, 11th should fail due to rate limit
        // We can't test actual success without Twilio, but we can verify the 11th has a rate limit error
        var lastResult = await service.SendSmsAsync("+15559999999", "Message 11");
        lastResult.Success.Should().BeFalse();
        lastResult.ErrorMessage.Should().Contain("Rate limit exceeded");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendMmsAsync_ExceedingRateLimit_ShouldReturnFailure()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15552222222" // Different from number to avoid cross-contamination
        };
        var service = CreateService(options);

        // Act - Attempt to send 11 MMS messages (max is 10 per minute)
        for (int i = 0; i < 10; i++)
        {
            await service.SendMmsAsync(
                "+15558888888",
                $"MMS {i}",
                new[] { "https://example.com/image.jpg" });
        }

        var lastResult = await service.SendMmsAsync(
            "+15558888888",
            "MMS 11",
            new[] { "https://example.com/image.jpg" });

        // Assert
        lastResult.Success.Should().BeFalse();
        lastResult.ErrorMessage.Should().Contain("Rate limit exceeded");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetDeliveryStatusAsync Tests

    [Fact]
    public async Task GetDeliveryStatusAsync_WithUnconfiguredService_ShouldReturnUnknown()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = null,
            AuthToken = null,
            FromNumber = null
        };
        var service = CreateService(options);

        // Act
        var status = await service.GetDeliveryStatusAsync("SM1234567890abcdef");

        // Assert
        status.Should().Be(Application.Interfaces.SmsStatus.Unknown);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMS service not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDeliveryStatusAsync_WithEmptyMessageId_ShouldReturnUnknown()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var status = await service.GetDeliveryStatusAsync("");

        // Assert
        status.Should().Be(Application.Interfaces.SmsStatus.Unknown);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("message ID is empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDeliveryStatusAsync_WithNullMessageId_ShouldReturnUnknown()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var status = await service.GetDeliveryStatusAsync(null!);

        // Assert
        status.Should().Be(Application.Interfaces.SmsStatus.Unknown);
    }

    [Fact]
    public async Task GetDeliveryStatusAsync_WithWhitespaceMessageId_ShouldReturnUnknown()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };
        var service = CreateService(options);

        // Act
        var status = await service.GetDeliveryStatusAsync("   ");

        // Assert
        status.Should().Be(Application.Interfaces.SmsStatus.Unknown);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldSucceed()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid-placeholder",
            AuthToken = "auth_token_12345678901234567890",
            FromNumber = "+15551234567"
        };

        // Act
        var service = CreateService(options);

        // Assert
        service.Should().NotBeNull();
        service.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldNotThrow()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<TwilioOptions>>();
        mockOptions.Setup(o => o.Value).Returns((TwilioOptions)null!);

        // Act
        var act = () => new TwilioSmsService(mockOptions.Object, _mockLogger.Object);

        // Assert - Constructor should not throw, but IsConfigured will be false
        act.Should().NotThrow();
    }

    #endregion
}
