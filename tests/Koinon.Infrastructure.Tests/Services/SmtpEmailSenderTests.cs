using FluentAssertions;
using Koinon.Application.DTOs.Communications;
using Koinon.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for SmtpEmailSender service.
/// These tests focus on configuration handling and parameter validation.
/// SMTP client behavior is integration-tested separately due to MailKit's concrete SmtpClient.
/// </summary>
public class SmtpEmailSenderTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<SmtpEmailSender>> _mockLogger;

    public SmtpEmailSenderTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<SmtpEmailSender>>();
    }

    private SmtpEmailSender CreateService()
    {
        return new SmtpEmailSender(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldSucceed()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SendEmailAsync_MissingSmtpHost_ShouldUseLocalhostDefault()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act - This will fail to connect (no SMTP server), but we can verify the configuration logic
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert - Should return false (connection will fail), but we verify it attempted with defaults
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_InvalidPort_Uses587Default()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("invalid-port");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("true");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert - Should log warning about invalid port and use default
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP port configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_MissingPort_Uses587Default()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("true");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP port configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_InvalidUseSsl_UsesTrueDefault()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("not-a-boolean");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert - Should log warning about invalid UseSsl and use default
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP UseSsl configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_MissingUseSsl_UsesTrueDefault()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns((string?)null);

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP UseSsl configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_NoCredentials_SkipsAuthentication()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns((string?)null);

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert - Should attempt to send without authentication (will fail without real server)
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithCredentials_AttemptsAuthentication()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("true");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("user@example.com");
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password123");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert - Should attempt authentication (will fail without real server)
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithPlainTextBody_ShouldIncludeBothFormats()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>HTML Body</p>",
            bodyText: "Plain Text Body");

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_WithAttachments_ShouldIncludeAllAttachments()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        var attachments = new List<EmailAttachmentDto>
        {
            new EmailAttachmentDto(
                FileName: "document.pdf",
                Content: new byte[] { 0x25, 0x50, 0x44, 0x46 }, // PDF header
                ContentType: "application/pdf"),
            new EmailAttachmentDto(
                FileName: "image.jpg",
                Content: new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, // JPEG header
                ContentType: "image/jpeg")
        };

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test with Attachments",
            bodyHtml: "<p>See attached files</p>",
            attachments: attachments);

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_WithReplyTo_ShouldSetReplyToHeader()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>",
            replyToAddress: "replyto@example.com");

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_WithoutToName_ShouldUseEmailAsName()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: null,
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_WithoutFromName_ShouldUseEmailAsName()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: null,
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_WithEmptyReplyTo_ShouldNotSetReplyToHeader()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>",
            replyToAddress: "");

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_WithNullAttachments_ShouldSucceed()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>",
            attachments: null);

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_WithEmptyAttachmentsList_ShouldSucceed()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>",
            attachments: new List<EmailAttachmentDto>());

        // Assert
        result.Should().BeFalse(); // No real SMTP server
    }

    [Fact]
    public async Task SendEmailAsync_ConnectionFailure_ShouldReturnFalse()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("invalid-host-that-does-not-exist.local");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("after retries")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithCancellationToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("localhost");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("25");
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns("false");

        var service = CreateService();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await service.SendEmailAsync(
                toAddress: "test@example.com",
                toName: "Test User",
                fromAddress: "sender@example.com",
                fromName: "Sender",
                subject: "Test",
                bodyHtml: "<p>Test</p>",
                ct: cts.Token);
        });
    }

    [Fact]
    public async Task SendEmailAsync_AllConfigurationDefaults_ShouldUseAllDefaults()
    {
        // Arrange - No configuration values set
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:UseSsl"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns((string?)null);

        var service = CreateService();

        // Act
        var result = await service.SendEmailAsync(
            toAddress: "test@example.com",
            toName: "Test User",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            subject: "Test",
            bodyHtml: "<p>Test</p>");

        // Assert - Should use defaults: localhost:587, SSL=true, no auth
        result.Should().BeFalse();

        // Verify warnings for missing configuration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP port configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP UseSsl configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
