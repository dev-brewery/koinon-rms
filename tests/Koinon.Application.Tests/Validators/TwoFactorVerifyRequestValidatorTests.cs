using FluentValidation.TestHelper;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Validators;
using Xunit;

namespace Koinon.Application.Tests.Validators;

/// <summary>
/// Tests for TwoFactorVerifyRequestValidator.
/// </summary>
public class TwoFactorVerifyRequestValidatorTests
{
    private readonly TwoFactorVerifyRequestValidator _validator;

    public TwoFactorVerifyRequestValidatorTests()
    {
        _validator = new TwoFactorVerifyRequestValidator();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Code_Is_Valid_6_Digits()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "123456" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("123456")]
    [InlineData("654321")]
    public void Should_Not_Have_Error_When_Code_Is_Valid_6_Digits_Theory(string code)
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = code };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Empty()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Verification code is required");
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Null()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = null! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Verification code is required");
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Whitespace()
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = "   " };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("12345")]   // Too short (5 digits)
    [InlineData("1234")]    // Too short (4 digits)
    [InlineData("123")]     // Too short (3 digits)
    [InlineData("12")]      // Too short (2 digits)
    [InlineData("1")]       // Too short (1 digit)
    public void Should_Have_Error_When_Code_Is_Too_Short(string code)
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = code };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Verification code must be 6 digits");
    }

    [Theory]
    [InlineData("1234567")]    // Too long (7 digits)
    [InlineData("12345678")]   // Too long (8 digits)
    [InlineData("123456789")]  // Too long (9 digits)
    public void Should_Have_Error_When_Code_Is_Too_Long(string code)
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = code };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Verification code must be 6 digits");
    }

    [Theory]
    [InlineData("abcdef")]     // Letters
    [InlineData("12345a")]     // Mix of digits and letters
    [InlineData("123 456")]    // Contains space
    [InlineData("123-456")]    // Contains hyphen
    [InlineData("123.456")]    // Contains period
    [InlineData("!@#$%^")]     // Special characters
    [InlineData("12345\n")]    // Contains newline
    [InlineData("12345\t")]    // Contains tab
    public void Should_Have_Error_When_Code_Contains_Non_Digits(string code)
    {
        // Arrange
        var request = new TwoFactorVerifyRequest { Code = code };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Verification code must be 6 digits");
    }
}
