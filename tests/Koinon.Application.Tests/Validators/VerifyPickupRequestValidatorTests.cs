using FluentValidation.TestHelper;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Validators;
using Xunit;

namespace Koinon.Application.Tests.Validators;

/// <summary>
/// Tests for VerifyPickupRequestValidator.
/// </summary>
public class VerifyPickupRequestValidatorTests
{
    private readonly VerifyPickupRequestValidator _validator;

    public VerifyPickupRequestValidatorTests()
    {
        _validator = new VerifyPickupRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_AttendanceIdKey_Is_Empty()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: string.Empty,
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            SecurityCode: "1234"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttendanceIdKey)
            .WithErrorMessage("AttendanceIdKey is required");
    }

    [Fact]
    public void Should_Have_Error_When_SecurityCode_Is_Empty()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            SecurityCode: string.Empty
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SecurityCode)
            .WithErrorMessage("SecurityCode is required");
    }

    [Fact]
    public void Should_Have_Error_When_SecurityCode_Exceeds_MaxLength()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            SecurityCode: "12345678901"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SecurityCode)
            .WithErrorMessage("SecurityCode cannot exceed 10 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_SecurityCode_Is_Within_MaxLength()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            SecurityCode: "1234567890"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SecurityCode);
    }

    [Fact]
    public void Should_Have_Error_When_Both_PickupPersonIdKey_And_PickupPersonName_Are_Empty()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: null,
            PickupPersonName: null,
            SecurityCode: "1234"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Either PickupPersonIdKey or PickupPersonName should be provided");
    }

    [Fact]
    public void Should_Not_Have_Error_When_PickupPersonIdKey_Is_Provided()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            SecurityCode: "1234"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PickupPersonName_Is_Provided()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: null,
            PickupPersonName: "John Doe",
            SecurityCode: "1234"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Both_PickupPersonIdKey_And_PickupPersonName_Are_Provided()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: "John Doe",
            SecurityCode: "1234"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Request()
    {
        // Arrange
        var request = new VerifyPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: "John Doe",
            SecurityCode: "1234"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
