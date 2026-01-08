using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

/// <summary>
/// Integration tests for UserSettingsController 2FA functionality.
/// </summary>
public class UserSettingsControllerTests
{
    private readonly Mock<IUserSettingsService> _userSettingsServiceMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<ILogger<UserSettingsController>> _loggerMock;
    private readonly UserSettingsController _controller;

    private const int TestPersonId = 123;

    public UserSettingsControllerTests()
    {
        _userSettingsServiceMock = new Mock<IUserSettingsService>();
        _userContextMock = new Mock<IUserContext>();
        _loggerMock = new Mock<ILogger<UserSettingsController>>();

        // Setup user context to return test person ID
        _userContextMock.Setup(x => x.CurrentPersonId).Returns(TestPersonId);
        _userContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        _controller = new UserSettingsController(
            _userSettingsServiceMock.Object,
            _userContextMock.Object,
            _loggerMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetTwoFactorStatus Tests

    [Fact]
    public async Task GetTwoFactorStatus_WhenEnabled_ReturnsOkWithStatus()
    {
        // Arrange
        var expectedStatus = new TwoFactorStatusDto
        {
            IsEnabled = true,
            EnabledAt = DateTime.UtcNow.AddDays(-7)
        };

        _userSettingsServiceMock
            .Setup(s => s.GetTwoFactorStatusAsync(TestPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorStatusDto>.Success(expectedStatus));

        // Act
        var result = await _controller.GetTwoFactorStatus(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();

        // Use reflection to get the data property from anonymous object
        var dataProperty = value!.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull();
        var status = dataProperty!.GetValue(value) as TwoFactorStatusDto;
        status.Should().NotBeNull();
        status!.IsEnabled.Should().BeTrue();
        status.EnabledAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-7), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetTwoFactorStatus_WhenDisabled_ReturnsOkWithStatus()
    {
        // Arrange
        var expectedStatus = new TwoFactorStatusDto
        {
            IsEnabled = false,
            EnabledAt = null
        };

        _userSettingsServiceMock
            .Setup(s => s.GetTwoFactorStatusAsync(TestPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorStatusDto>.Success(expectedStatus));

        // Act
        var result = await _controller.GetTwoFactorStatus(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();

        // Use reflection to get the data property from anonymous object
        var dataProperty = value!.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull();
        var status = dataProperty!.GetValue(value) as TwoFactorStatusDto;
        status.Should().NotBeNull();
        status!.IsEnabled.Should().BeFalse();
        status.EnabledAt.Should().BeNull();
    }

    [Fact]
    public async Task GetTwoFactorStatus_WhenServiceFails_ReturnsProblemDetails()
    {
        // Arrange
        var error = Error.Validation("Test error");
        _userSettingsServiceMock
            .Setup(s => s.GetTwoFactorStatusAsync(TestPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorStatusDto>.Failure(error));

        // Act
        var result = await _controller.GetTwoFactorStatus(CancellationToken.None);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion

    #region SetupTwoFactor Tests

    [Fact]
    public async Task SetupTwoFactor_WhenSuccessful_ReturnsOkWithSetupData()
    {
        // Arrange
        var expectedSetup = new TwoFactorSetupDto
        {
            SecretKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567",
            QrCodeUri = "otpauth://totp/Koinon%20RMS:test@example.com?secret=ABC&issuer=Koinon%20RMS",
            RecoveryCodes = new List<string> { "CODE1234", "CODE5678" }.AsReadOnly()
        };

        _userSettingsServiceMock
            .Setup(s => s.SetupTwoFactorAsync(TestPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorSetupDto>.Success(expectedSetup));

        // Act
        var result = await _controller.SetupTwoFactor(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();

        // Use reflection to get the data property from anonymous object
        var dataProperty = value!.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull();
        var setup = dataProperty!.GetValue(value) as TwoFactorSetupDto;
        setup.Should().NotBeNull();
        setup!.SecretKey.Should().Be(expectedSetup.SecretKey);
        setup.QrCodeUri.Should().Contain("otpauth://totp/");
        setup.RecoveryCodes.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetupTwoFactor_WhenPersonNotFound_ReturnsError()
    {
        // Arrange
        var error = Error.NotFound("Person.NotFound", "Person not found");
        _userSettingsServiceMock
            .Setup(s => s.SetupTwoFactorAsync(TestPersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorSetupDto>.Failure(error));

        // Act
        var result = await _controller.SetupTwoFactor(CancellationToken.None);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion

    #region VerifyTwoFactor Tests

    [Fact]
    public async Task VerifyTwoFactor_WithValidCode_ReturnsNoContent()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "123456" };

        _userSettingsServiceMock
            .Setup(s => s.VerifyTwoFactorAsync(TestPersonId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.VerifyTwoFactor(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task VerifyTwoFactor_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "000000" };

        var error = Error.Validation("Invalid two-factor authentication code");
        _userSettingsServiceMock
            .Setup(s => s.VerifyTwoFactorAsync(TestPersonId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.VerifyTwoFactor(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Invalid two-factor authentication code");
    }

    [Fact]
    public async Task VerifyTwoFactor_WhenConfigNotFound_ReturnsError()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "123456" };

        var error = Error.NotFound("TwoFactorConfig.NotFound", "Two-factor configuration not found");
        _userSettingsServiceMock
            .Setup(s => s.VerifyTwoFactorAsync(TestPersonId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.VerifyTwoFactor(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region DisableTwoFactor Tests

    [Fact]
    public async Task DisableTwoFactor_WithValidCode_ReturnsNoContent()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "123456" };

        _userSettingsServiceMock
            .Setup(s => s.DisableTwoFactorAsync(TestPersonId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.DisableTwoFactor(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DisableTwoFactor_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "000000" };

        var error = Error.Validation("Invalid two-factor authentication code");
        _userSettingsServiceMock
            .Setup(s => s.DisableTwoFactorAsync(TestPersonId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.DisableTwoFactor(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Invalid two-factor authentication code");
    }

    [Fact]
    public async Task DisableTwoFactor_WhenConfigNotFound_ReturnsError()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "123456" };

        var error = Error.NotFound("TwoFactorConfig.NotFound", "Two-factor configuration not found");
        _userSettingsServiceMock
            .Setup(s => s.DisableTwoFactorAsync(TestPersonId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.DisableTwoFactor(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion
}
