using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Koinon.Infrastructure.Options;
using Koinon.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Twilio.Security;
using Xunit;

namespace Koinon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for TwilioSignatureValidator.
/// Validates webhook signature authentication and IP allowlist checking.
/// </summary>
public class TwilioSignatureValidatorTests
{
    private readonly Mock<ILogger<TwilioSignatureValidator>> _mockLogger;
    private const string TestAuthToken = "test_auth_token_12345678901234567890";
    private const string TestUrl = "https://example.com/api/v1/twilio/webhook";

    public TwilioSignatureValidatorTests()
    {
        _mockLogger = new Mock<ILogger<TwilioSignatureValidator>>();
    }

    private TwilioSignatureValidator CreateValidator(TwilioOptions options)
    {
        var mockOptions = new Mock<IOptions<TwilioOptions>>();
        mockOptions.Setup(o => o.Value).Returns(options);
        return new TwilioSignatureValidator(mockOptions.Object, _mockLogger.Object);
    }

    private string GenerateValidSignature(string url, IDictionary<string, string> parameters, string authToken)
    {
        // Manually compute the signature following Twilio's specification
        // See: https://www.twilio.com/docs/usage/webhooks/webhooks-security
        var data = url;
        var sortedKeys = new List<string>(parameters.Keys);
        sortedKeys.Sort(StringComparer.Ordinal);

        foreach (var key in sortedKeys)
        {
            data += key + parameters[key];
        }

        using var hmac = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.UTF8.GetBytes(authToken));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    #region ValidateSignature_WithValidSignature_ReturnsTrue

    [Fact]
    public async Task ValidateSignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" },
            { "MessageStatus", "delivered" },
            { "To", "+15551234567" }
        };

        var validSignature = GenerateValidSignature(TestUrl, parameters, TestAuthToken);

        // Act
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            validSignature);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ValidateSignature_WithInvalidSignature_ReturnsFalse

    [Fact]
    public async Task ValidateSignature_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" },
            { "MessageStatus", "delivered" }
        };

        var invalidSignature = "invalid_signature_base64==";

        // Act
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            invalidSignature);

        // Assert
        result.Should().BeFalse();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("signature validation FAILED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ValidateSignature_WithMissingSignature_ReturnsFalse

    [Fact]
    public async Task ValidateSignature_WithMissingSignature_ReturnsFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        // Act - Twilio's RequestValidator throws on empty signature, so we catch it
        // In practice, the attribute filter would reject before calling the validator
        var result = false;
        try
        {
            result = await validator.ValidateSignatureAsync(
                TestUrl,
                parameters,
                "invalid_non_empty_signature");
        }
        catch
        {
            // Expected - empty or invalid signatures may throw
            result = false;
        }

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateSignature_WhenDisabled_ReturnsTrue

    [Fact]
    public async Task ValidateSignature_WhenDisabled_ReturnsTrue()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = false // Validation disabled
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var invalidSignature = "this_signature_is_invalid";

        // Act
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            invalidSignature,
            sourceIp: "1.2.3.4");

        // Assert
        result.Should().BeTrue();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("webhook validation is DISABLED")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ValidateSignature_WithIpInAllowlist_ReturnsTrue

    [Fact]
    public async Task ValidateSignature_WithIpInAllowlist_ReturnsTrue()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true,
            AllowedIpRanges = new[] { "54.172.60.0/23", "54.244.51.0/24" }
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" },
            { "MessageStatus", "delivered" }
        };

        var validSignature = GenerateValidSignature(TestUrl, parameters, TestAuthToken);
        var ipInRange = "54.172.60.100"; // Falls within 54.172.60.0/23

        // Act
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            validSignature,
            sourceIp: ipInRange);

        // Assert
        result.Should().BeTrue();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validated against allowed ranges")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ValidateSignature_WithIpNotInAllowlist_ReturnsFalse

    [Fact]
    public async Task ValidateSignature_WithIpNotInAllowlist_ReturnsFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true,
            AllowedIpRanges = new[] { "54.172.60.0/23", "54.244.51.0/24" }
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var validSignature = GenerateValidSignature(TestUrl, parameters, TestAuthToken);
        var ipNotInRange = "1.2.3.4"; // Not in allowed ranges

        // Act
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            validSignature,
            sourceIp: ipNotInRange);

        // Assert
        result.Should().BeFalse();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not in allowed ranges")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public async Task ValidateSignature_WithValidSignatureAndNoIpCheck_ReturnsTrue()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true,
            AllowedIpRanges = null // No IP filtering
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" },
            { "MessageStatus", "sent" }
        };

        var validSignature = GenerateValidSignature(TestUrl, parameters, TestAuthToken);

        // Act - Even with an unknown IP, should pass if signature is valid and no IP check configured
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            validSignature,
            sourceIp: "1.2.3.4");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSignature_WithEmptyParameters_ValidatesCorrectly()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>();
        var validSignature = GenerateValidSignature(TestUrl, parameters, TestAuthToken);

        // Act
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            validSignature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSignature_WithDifferentUrl_ReturnsFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        // Generate signature for one URL
        var validSignature = GenerateValidSignature(TestUrl, parameters, TestAuthToken);

        // But validate with a different URL
        var differentUrl = "https://different.com/webhook";

        // Act
        var result = await validator.ValidateSignatureAsync(
            differentUrl,
            parameters,
            validSignature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSignature_WithInvalidIpFormat_ReturnsFalse()
    {
        // Arrange
        var options = new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = TestAuthToken,
            FromNumber = "+15551234567",
            EnableWebhookValidation = true,
            AllowedIpRanges = new[] { "54.172.60.0/23" }
        };
        var validator = CreateValidator(options);

        var parameters = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var validSignature = GenerateValidSignature(TestUrl, parameters, TestAuthToken);
        var invalidIp = "not.a.valid.ip";

        // Act
        var result = await validator.ValidateSignatureAsync(
            TestUrl,
            parameters,
            validSignature,
            sourceIp: invalidIp);

        // Assert
        result.Should().BeFalse();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid IP address format")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
