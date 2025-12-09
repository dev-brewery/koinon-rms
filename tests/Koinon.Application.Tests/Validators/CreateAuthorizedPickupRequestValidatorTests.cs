using FluentValidation.TestHelper;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Validators;
using Koinon.Domain.Enums;
using Xunit;

namespace Koinon.Application.Tests.Validators;

/// <summary>
/// Tests for CreateAuthorizedPickupRequestValidator.
/// </summary>
public class CreateAuthorizedPickupRequestValidatorTests
{
    private readonly CreateAuthorizedPickupRequestValidator _validator;

    public CreateAuthorizedPickupRequestValidatorTests()
    {
        _validator = new CreateAuthorizedPickupRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Both_AuthorizedPersonIdKey_And_Name_Are_Empty()
    {
        // Arrange
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: null,
            Name: null,
            PhoneNumber: null,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Either AuthorizedPersonIdKey or Name must be provided");
    }

    [Fact]
    public void Should_Not_Have_Error_When_AuthorizedPersonIdKey_Is_Provided()
    {
        // Arrange
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: "ABC123",
            Name: null,
            PhoneNumber: null,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Name_Is_Provided()
    {
        // Arrange
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: null,
            Name: "John Doe",
            PhoneNumber: null,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var longName = new string('A', 201);
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: null,
            Name: longName,
            PhoneNumber: null,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 200 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Name_Is_Within_MaxLength()
    {
        // Arrange
        var validName = new string('A', 200);
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: null,
            Name: validName,
            PhoneNumber: null,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("+12345678901")]
    [InlineData("+442071234567")]
    [InlineData("12345678901")]
    [InlineData("(555) 123-4567")]
    [InlineData("555-123-4567")]
    [InlineData("555.123.4567")]
    public void Should_Not_Have_Error_For_Valid_PhoneNumber_Format(string phoneNumber)
    {
        // Arrange
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: "ABC123",
            Name: null,
            PhoneNumber: phoneNumber,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0123456789")]
    [InlineData("+0123456789")]
    [InlineData("+1234567890123456")]
    [InlineData("not-a-phone")]
    [InlineData("123")]
    public void Should_Have_Error_For_Invalid_PhoneNumber_Format(string phoneNumber)
    {
        // Arrange
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: "ABC123",
            Name: null,
            PhoneNumber: phoneNumber,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Phone number must be in valid E.164 format (e.g., +12345678901)");
    }

    [Fact]
    public void Should_Have_Error_When_PhotoUrl_Exceeds_MaxLength()
    {
        // Arrange
        var longUrl = new string('A', 501);
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: "ABC123",
            Name: null,
            PhoneNumber: null,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: longUrl,
            CustodyNotes: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhotoUrl)
            .WithErrorMessage("PhotoUrl cannot exceed 500 characters");
    }

    [Fact]
    public void Should_Have_Error_When_CustodyNotes_Exceeds_MaxLength()
    {
        // Arrange
        var longNotes = new string('A', 2001);
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: "ABC123",
            Name: null,
            PhoneNumber: null,
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: null,
            CustodyNotes: longNotes
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustodyNotes)
            .WithErrorMessage("CustodyNotes cannot exceed 2000 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Request()
    {
        // Arrange
        var request = new CreateAuthorizedPickupRequest(
            AuthorizedPersonIdKey: "ABC123",
            Name: "John Doe",
            PhoneNumber: "+12345678901",
            Relationship: PickupRelationship.Parent,
            AuthorizationLevel: AuthorizationLevel.Always,
            PhotoUrl: "https://example.com/photo.jpg",
            CustodyNotes: "Some notes"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
