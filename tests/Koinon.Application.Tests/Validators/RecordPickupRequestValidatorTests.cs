using FluentValidation.TestHelper;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Validators;
using Xunit;

namespace Koinon.Application.Tests.Validators;

/// <summary>
/// Tests for RecordPickupRequestValidator.
/// </summary>
public class RecordPickupRequestValidatorTests
{
    private readonly RecordPickupRequestValidator _validator;

    public RecordPickupRequestValidatorTests()
    {
        _validator = new RecordPickupRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_AttendanceIdKey_Is_Empty()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: string.Empty,
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: true,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttendanceIdKey)
            .WithErrorMessage("AttendanceIdKey is required");
    }

    [Fact]
    public void Should_Have_Error_When_Both_PickupPersonIdKey_And_PickupPersonName_Are_Empty()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: null,
            PickupPersonName: null,
            WasAuthorized: true,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Either PickupPersonIdKey or PickupPersonName must be provided");
    }

    [Fact]
    public void Should_Not_Have_Error_When_PickupPersonIdKey_Is_Provided()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: true,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: null
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
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: null,
            PickupPersonName: "John Doe",
            WasAuthorized: true,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_Have_Error_When_SupervisorOverride_Is_True_But_SupervisorPersonIdKey_Is_Empty()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: false,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: true,
            SupervisorPersonIdKey: null,
            Notes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SupervisorPersonIdKey)
            .WithErrorMessage("SupervisorPersonIdKey is required when SupervisorOverride is true");
    }

    [Fact]
    public void Should_Not_Have_Error_When_SupervisorOverride_Is_True_And_SupervisorPersonIdKey_Is_Provided()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: false,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: true,
            SupervisorPersonIdKey: "SUP123",
            Notes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SupervisorPersonIdKey);
    }

    [Fact]
    public void Should_Have_Error_When_WasAuthorized_Is_False_And_SupervisorOverride_Is_False()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: false,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SupervisorOverride)
            .WithErrorMessage("SupervisorOverride should be true when WasAuthorized is false");
    }

    [Fact]
    public void Should_Not_Have_Error_When_WasAuthorized_Is_False_And_SupervisorOverride_Is_True()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: false,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: true,
            SupervisorPersonIdKey: "SUP123",
            Notes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SupervisorOverride);
    }

    [Fact]
    public void Should_Have_Error_When_Notes_Exceeds_MaxLength()
    {
        // Arrange
        var longNotes = new string('A', 1001);
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: true,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: longNotes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Notes_Is_Within_MaxLength()
    {
        // Arrange
        var validNotes = new string('A', 1000);
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: null,
            WasAuthorized: true,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: validNotes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Authorized_Pickup_Request()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: "John Doe",
            WasAuthorized: true,
            AuthorizedPickupIdKey: "AUTH123",
            SupervisorOverride: false,
            SupervisorPersonIdKey: null,
            Notes: "Child picked up by parent"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Unauthorized_Pickup_With_Override()
    {
        // Arrange
        var request = new RecordPickupRequest(
            AttendanceIdKey: "ATT123",
            PickupPersonIdKey: "ABC123",
            PickupPersonName: "Jane Smith",
            WasAuthorized: false,
            AuthorizedPickupIdKey: null,
            SupervisorOverride: true,
            SupervisorPersonIdKey: "SUP123",
            Notes: "Emergency pickup approved by supervisor"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
