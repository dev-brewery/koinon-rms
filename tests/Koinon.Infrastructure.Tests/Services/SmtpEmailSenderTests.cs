using FluentAssertions;
using Koinon.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Infrastructure.Tests.Services;

public class SmtpEmailSenderTests
{
    private readonly Mock<ILogger<SmtpEmailSender>> _loggerMock;
    private readonly IConfiguration _configuration;

    public SmtpEmailSenderTests()
    {
        _loggerMock = new Mock<ILogger<SmtpEmailSender>>();
        var configData = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "587",
            ["Smtp:Username"] = "testuser",
            ["Smtp:Password"] = "testpass",
            ["Smtp:UseSsl"] = "true"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public async Task SendEmailAsync_WithValidParameters_AttemptsToSend()
    {
        var sender = new SmtpEmailSender(_configuration, _loggerMock.Object);
        var result = await sender.SendEmailAsync(
            toAddress: "recipient@example.com",
            toName: "Test Recipient",
            fromAddress: "sender@example.com",
            fromName: "Test Sender",
            subject: "Test Subject",
            bodyHtml: "<p>Test Body</p>",
            replyToAddress: null,
            ct: CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithInvalidSmtpPort_UsesDefault587()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "invalid",
            ["Smtp:Username"] = "testuser",
            ["Smtp:Password"] = "testpass",
            ["Smtp:UseSsl"] = "true"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var sender = new SmtpEmailSender(configuration, _loggerMock.Object);
        var result = await sender.SendEmailAsync(
            toAddress: "recipient@example.com",
            toName: "Test Recipient",
            fromAddress: "sender@example.com",
            fromName: "Test Sender",
            subject: "Test Subject",
            bodyHtml: "<p>Test Body</p>");
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP port configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithInvalidUseSsl_UsesDefaultTrue()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "587",
            ["Smtp:Username"] = "testuser",
            ["Smtp:Password"] = "testpass",
            ["Smtp:UseSsl"] = "invalid-boolean"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var sender = new SmtpEmailSender(configuration, _loggerMock.Object);
        var result = await sender.SendEmailAsync(
            toAddress: "recipient@example.com",
            toName: null,
            fromAddress: "sender@example.com",
            fromName: null,
            subject: "Test Subject",
            bodyHtml: "<p>Test Body</p>");
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid SMTP UseSsl configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithConnectionFailure_LogsErrorAndReturnsFalse()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "invalid.smtp.server.that.does.not.exist",
            ["Smtp:Port"] = "587",
            ["Smtp:Username"] = "testuser",
            ["Smtp:Password"] = "testpass",
            ["Smtp:UseSsl"] = "true"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var sender = new SmtpEmailSender(configuration, _loggerMock.Object);
        var result = await sender.SendEmailAsync(
            toAddress: "recipient@example.com",
            toName: "Test Recipient",
            fromAddress: "sender@example.com",
            fromName: "Test Sender",
            subject: "Test Subject",
            bodyHtml: "<p>Test Body</p>");
        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendEmailAsync_WithCancellationToken_RespectsCancellation()
    {
        var sender = new SmtpEmailSender(_configuration, _loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await sender.SendEmailAsync(
                toAddress: "recipient@example.com",
                toName: "Test Recipient",
                fromAddress: "sender@example.com",
                fromName: "Test Sender",
                subject: "Test Subject",
                bodyHtml: "<p>Test Body</p>",
                replyToAddress: null,
                ct: cts.Token);
        });
    }
}
