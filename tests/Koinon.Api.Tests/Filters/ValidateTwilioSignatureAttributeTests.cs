using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Koinon.Api.Filters;
using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Filters;

/// <summary>
/// Tests for ValidateTwilioSignatureAttribute authorization filter.
/// Validates that the filter correctly enforces signature validation for Twilio webhooks.
/// </summary>
public class ValidateTwilioSignatureAttributeTests
{
    private readonly Mock<ITwilioSignatureValidator> _mockValidator;
    private readonly Mock<IOptions<TwilioOptions>> _mockOptions;
    private readonly ValidateTwilioSignatureAttribute _attribute;

    public ValidateTwilioSignatureAttributeTests()
    {
        _mockValidator = new Mock<ITwilioSignatureValidator>();
        _mockOptions = new Mock<IOptions<TwilioOptions>>();
        _attribute = new ValidateTwilioSignatureAttribute();

        // Default: validation enabled
        _mockOptions.Setup(o => o.Value).Returns(new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = "test-auth-token",
            FromNumber = "+15551234567",
            EnableWebhookValidation = true
        });
    }

    private AuthorizationFilterContext CreateAuthorizationContext(
        string? signatureHeader = null,
        Dictionary<string, string>? formData = null)
    {
        var httpContext = new DefaultHttpContext();

        // Setup service provider with mocked dependencies
        var services = new ServiceCollection();
        services.AddSingleton(_mockValidator.Object);
        services.AddSingleton(_mockOptions.Object);
        httpContext.RequestServices = services.BuildServiceProvider();

        // Setup request
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.Path = "/api/v1/twilio/webhook";
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";

        // Add signature header if provided
        if (signatureHeader != null)
        {
            httpContext.Request.Headers["X-Twilio-Signature"] = signatureHeader;
        }

        // Setup form data if provided
        if (formData != null)
        {
            var formCollection = new FormCollection(
                formData.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new Microsoft.Extensions.Primitives.StringValues(kvp.Value)));

            httpContext.Request.Form = formCollection;
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());
    }

    #region OnAuthorizationAsync_WithValidSignature_AllowsRequest

    [Fact]
    public async Task OnAuthorizationAsync_WithValidSignature_AllowsRequest()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" },
            { "MessageStatus", "delivered" },
            { "To", "+15551234567" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: "valid_signature_base64==",
            formData: formData);

        _mockValidator
            .Setup(v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull(); // Null result means request is allowed

        _mockValidator.Verify(
            v => v.ValidateSignatureAsync(
                "https://example.com/api/v1/twilio/webhook",
                It.Is<IDictionary<string, string>>(p =>
                    p["MessageSid"] == "SM1234567890abcdef1234567890abcdef" &&
                    p["MessageStatus"] == "delivered"),
                "valid_signature_base64==",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region OnAuthorizationAsync_WithInvalidSignature_Returns403

    [Fact]
    public async Task OnAuthorizationAsync_WithInvalidSignature_Returns403()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: "invalid_signature",
            formData: formData);

        _mockValidator
            .Setup(v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>();
        var objectResult = context.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Invalid Twilio signature");
        problemDetails.Status.Should().Be(StatusCodes.Status403Forbidden);
        problemDetails.Detail.Should().Contain("validation failed");
    }

    #endregion

    #region OnAuthorizationAsync_WithMissingHeader_Returns403

    [Fact]
    public async Task OnAuthorizationAsync_WithMissingHeader_Returns403()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: null, // No signature header
            formData: formData);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>();
        var objectResult = context.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Missing Twilio signature");
        problemDetails.Status.Should().Be(StatusCodes.Status403Forbidden);
        problemDetails.Detail.Should().Contain("X-Twilio-Signature header is required");

        // Validator should not be called if header is missing
        _mockValidator.Verify(
            v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region OnAuthorizationAsync_WhenValidationDisabled_AllowsRequest

    [Fact]
    public async Task OnAuthorizationAsync_WhenValidationDisabled_AllowsRequest()
    {
        // Arrange
        _mockOptions.Setup(o => o.Value).Returns(new TwilioOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = "test-auth-token",
            FromNumber = "+15551234567",
            EnableWebhookValidation = false // Validation disabled
        });

        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: "any_signature_should_work",
            formData: formData);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull(); // Request allowed without validation

        // Validator should not be called when validation is disabled
        _mockValidator.Verify(
            v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public async Task OnAuthorizationAsync_WithEmptySignatureHeader_Returns403()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: "", // Empty signature
            formData: formData);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>();
        var objectResult = context.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Missing Twilio signature");
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithWhitespaceSignatureHeader_Returns403()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: "   ", // Whitespace only
            formData: formData);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>();
        var objectResult = context.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Missing Twilio signature");
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithNoFormData_StillValidates()
    {
        // Arrange - Twilio might send minimal data in some cases
        var context = CreateAuthorizationContext(
            signatureHeader: "valid_signature_base64==",
            formData: new Dictionary<string, string>());

        _mockValidator
            .Setup(v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull(); // Request allowed

        _mockValidator.Verify(
            v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.Is<IDictionary<string, string>>(p => p.Count == 0),
                "valid_signature_base64==",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnAuthorizationAsync_ExtractsClientIpAddress()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: "valid_signature_base64==",
            formData: formData);

        // Set a remote IP address
        context.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("54.172.60.100");

        _mockValidator
            .Setup(v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        _mockValidator.Verify(
            v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                "54.172.60.100", // Should pass the IP address
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithQueryString_IncludesInUrl()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "MessageSid", "SM1234567890abcdef1234567890abcdef" }
        };

        var context = CreateAuthorizationContext(
            signatureHeader: "valid_signature_base64==",
            formData: formData);

        // Add query string
        context.HttpContext.Request.QueryString = new QueryString("?test=value");

        _mockValidator
            .Setup(v => v.ValidateSignatureAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _attribute.OnAuthorizationAsync(context);

        // Assert
        _mockValidator.Verify(
            v => v.ValidateSignatureAsync(
                "https://example.com/api/v1/twilio/webhook?test=value",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
