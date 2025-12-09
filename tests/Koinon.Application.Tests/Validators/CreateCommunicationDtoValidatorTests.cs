using FluentValidation.TestHelper;
using Koinon.Application.DTOs;
using Koinon.Application.Validators;
using Xunit;

namespace Koinon.Application.Tests.Validators;

public class CreateCommunicationDtoValidatorTests
{
    private readonly CreateCommunicationDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_GroupIdKeys_Is_Empty()
    {
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "test@example.com",
            GroupIdKeys = new List<string>()
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.GroupIdKeys)
            .WithErrorMessage("At least one group must be specified");
    }

    [Fact]
    public void Should_Have_Error_When_GroupIdKeys_Exceeds_50()
    {
        var groupIdKeys = Enumerable.Range(1, 51).Select(i => $"GROUP{i}").ToList();
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "test@example.com",
            GroupIdKeys = groupIdKeys
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.GroupIdKeys)
            .WithErrorMessage("Cannot specify more than 50 groups per communication");
    }

    [Fact]
    public void Should_Have_Error_When_Subject_Exceeds_MaxLength()
    {
        var longSubject = new string('A', 501);
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = longSubject,
            Body = "Test Body",
            FromEmail = "test@example.com",
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Subject)
            .WithErrorMessage("Subject cannot exceed 500 characters");
    }

    [Theory]
    [InlineData("Subject\nwith newline")]
    [InlineData("Subject\rwith carriage return")]
    [InlineData("Subject\0with null byte")]
    public void Should_Have_Error_When_Subject_Contains_HeaderInjection(string subject)
    {
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = subject,
            Body = "Test Body",
            FromEmail = "test@example.com",
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Subject)
            .WithErrorMessage("Subject contains invalid characters");
    }

    [Fact]
    public void Should_Have_Error_When_FromEmail_Is_Invalid()
    {
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "invalid-email",
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FromEmail)
            .WithErrorMessage("Invalid email address");
    }

    [Fact]
    public void Should_Have_Error_When_FromEmail_Exceeds_MaxLength()
    {
        var longEmail = new string('a', 250) + "@test.com";
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = longEmail,
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FromEmail)
            .WithErrorMessage("From email cannot exceed 254 characters");
    }

    [Fact]
    public void Should_Have_Error_When_FromName_Exceeds_MaxLength()
    {
        var longName = new string('A', 201);
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "test@example.com",
            FromName = longName,
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FromName)
            .WithErrorMessage("From name cannot exceed 200 characters");
    }

    [Fact]
    public void Should_Have_Error_When_ReplyToEmail_Is_Invalid()
    {
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "from@example.com",
            ReplyToEmail = "invalid-email",
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ReplyToEmail)
            .WithErrorMessage("Invalid email address");
    }

    [Fact]
    public void Should_Have_Error_When_ReplyToEmail_Exceeds_MaxLength()
    {
        var longEmail = new string('a', 250) + "@test.com";
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "from@example.com",
            ReplyToEmail = longEmail,
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ReplyToEmail)
            .WithErrorMessage("Reply-to email cannot exceed 254 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Email_Communication()
    {
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Valid Email Subject",
            Body = "Valid email body content",
            FromEmail = "sender@example.com",
            FromName = "Sender Name",
            ReplyToEmail = "reply@example.com",
            Note = "Some note",
            GroupIdKeys = new List<string> { "ABC123", "DEF456" }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
