using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Auth;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IValidator<LoginRequest>> _loginValidatorMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loginValidatorMock = new Mock<IValidator<LoginRequest>>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _authServiceMock.Object,
            _loginValidatorMock.Object,
            _loggerMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var expectedResponse = new TokenResponse(
            AccessToken: "access_token_jwt",
            RefreshToken: "refresh_token",
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            User: new PersonSummaryDto
            {
                IdKey = "abc123",
                FirstName = "Test",
                LastName = "User",
                FullName = "Test User",
                Gender = "Male",
                Email = "test@example.com"
            });

        _loginValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _authServiceMock
            .Setup(s => s.LoginAsync(request, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var tokenResponse = dataProperty!.GetValue(response).Should().BeOfType<TokenResponse>().Subject;

        tokenResponse.AccessToken.Should().Be("access_token_jwt");
        tokenResponse.RefreshToken.Should().Be("refresh_token");
        tokenResponse.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");

        _loginValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _authServiceMock
            .Setup(s => s.LoginAsync(request, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenResponse?)null);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problemDetails = unauthorizedResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problemDetails.Detail.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_WithValidationErrors_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest("invalid-email", "");
        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "Email is not valid"),
            new("Password", "Password is required")
        };

        _loginValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Errors.Should().ContainKey("Email");
        problemDetails.Errors.Should().ContainKey("Password");
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsOkWithNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest("valid_refresh_token");
        var expectedResponse = new TokenResponse(
            AccessToken: "new_access_token",
            RefreshToken: "new_refresh_token",
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            User: new PersonSummaryDto
            {
                IdKey = "abc123",
                FirstName = "Test",
                LastName = "User",
                FullName = "Test User",
                Gender = "Male",
                Email = "test@example.com"
            });

        _authServiceMock
            .Setup(s => s.RefreshTokenAsync("valid_refresh_token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var tokenResponse = dataProperty!.GetValue(response).Should().BeOfType<TokenResponse>().Subject;

        tokenResponse.AccessToken.Should().Be("new_access_token");
        tokenResponse.RefreshToken.Should().Be("new_refresh_token");
    }

    [Fact]
    public async Task RefreshToken_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequest("");

        // Act
        var result = await _controller.RefreshToken(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Refresh token is required");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid_or_expired_token");

        _authServiceMock
            .Setup(s => s.RefreshTokenAsync("invalid_or_expired_token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenResponse?)null);

        // Act
        var result = await _controller.RefreshToken(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problemDetails = unauthorizedResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problemDetails.Detail.Should().Contain("Invalid or expired refresh token");
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsNoContent()
    {
        // Arrange
        var request = new RefreshTokenRequest("valid_refresh_token");

        _authServiceMock
            .Setup(s => s.LogoutAsync("valid_refresh_token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Logout(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Logout_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequest("");

        // Act
        var result = await _controller.Logout(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Refresh token is required");
    }
}
