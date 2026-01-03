using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class MergeFieldServiceTests
{
    private readonly MergeFieldService _service;

    public MergeFieldServiceTests()
    {
        _service = new MergeFieldService();
    }

    #region GetAvailableMergeFields Tests

    [Fact]
    public void GetAvailableMergeFields_ReturnsExpectedFields()
    {
        // Act
        var fields = _service.GetAvailableMergeFields();

        // Assert
        fields.Should().HaveCount(5);
        fields.Should().Contain(f => f.Name == "FirstName" && f.Token == "{{FirstName}}");
        fields.Should().Contain(f => f.Name == "LastName" && f.Token == "{{LastName}}");
        fields.Should().Contain(f => f.Name == "NickName" && f.Token == "{{NickName}}");
        fields.Should().Contain(f => f.Name == "FullName" && f.Token == "{{FullName}}");
        fields.Should().Contain(f => f.Name == "Email" && f.Token == "{{Email}}");
    }

    [Fact]
    public void GetAvailableMergeFields_ReturnsReadOnlyList()
    {
        // Act
        var fields = _service.GetAvailableMergeFields();

        // Assert
        fields.Should().BeAssignableTo<IReadOnlyList<Koinon.Application.DTOs.MergeFieldDto>>();
    }

    [Fact]
    public void GetAvailableMergeFields_AllFieldsHaveDescriptions()
    {
        // Act
        var fields = _service.GetAvailableMergeFields();

        // Assert
        fields.Should().AllSatisfy(f => f.Description.Should().NotBeNullOrWhiteSpace());
    }

    #endregion

    #region ReplaceMergeFields Tests

    [Fact]
    public void ReplaceMergeFields_WithFirstName_ReplacesCorrectly()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "Hello {{FirstName}}!";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Hello John!");
    }

    [Fact]
    public void ReplaceMergeFields_WithLastName_ReplacesCorrectly()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "Dear {{LastName}},";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Dear Doe,");
    }

    [Fact]
    public void ReplaceMergeFields_WithNickName_WhenNickNameExists_UsesNickName()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Jonathan",
            LastName = "Doe",
            NickName = "Johnny"
        };
        var template = "Hi {{NickName}}!";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Hi Johnny!");
    }

    [Fact]
    public void ReplaceMergeFields_WithNickName_WhenNickNameIsNull_FallsBackToFirstName()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Jonathan",
            LastName = "Doe",
            NickName = null
        };
        var template = "Hi {{NickName}}!";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Hi Jonathan!");
    }

    [Fact]
    public void ReplaceMergeFields_WithNickName_WhenNickNameIsEmpty_FallsBackToFirstName()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Jonathan",
            LastName = "Doe",
            NickName = ""
        };
        var template = "Hi {{NickName}}!";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Hi Jonathan!");
    }

    [Fact]
    public void ReplaceMergeFields_WithFullName_ReplacesCorrectly()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "Welcome, {{FullName}}!";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Welcome, John Doe!");
    }

    [Fact]
    public void ReplaceMergeFields_WithEmail_ReplacesCorrectly()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };
        var template = "Send to: {{Email}}";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Send to: john.doe@example.com");
    }

    [Fact]
    public void ReplaceMergeFields_WithNullEmail_ReplacesWithEmptyString()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = null
        };
        var template = "Send to: {{Email}}";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Send to: ");
    }

    [Fact]
    public void ReplaceMergeFields_WithMultipleFields_ReplacesAll()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };
        var template = "Hello {{FirstName}} {{LastName}}, your email is {{Email}}.";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Hello John Doe, your email is john.doe@example.com.");
    }

    [Fact]
    public void ReplaceMergeFields_WithDuplicateFields_ReplacesAllInstances()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "{{FirstName}} {{FirstName}} {{FirstName}}";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("John John John");
    }

    [Fact]
    public void ReplaceMergeFields_WithUnknownField_LeavesTokenUnchanged()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "Hello {{UnknownField}}!";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("Hello {{UnknownField}}!");
    }

    [Fact]
    public void ReplaceMergeFields_WithEmptyTemplate_ReturnsEmptyString()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void ReplaceMergeFields_WithNullTemplate_ReturnsNull()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        string? template = null;

        // Act
        var result = _service.ReplaceMergeFields(template!, person);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReplaceMergeFields_WithNoMergeFields_ReturnsOriginalText()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "This is plain text with no merge fields.";

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert
        result.Should().Be("This is plain text with no merge fields.");
    }

    [Fact]
    public void ReplaceMergeFields_WithCaseSensitiveFieldName_OnlyMatchesExactCase()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var template = "Hello {{firstname}}!"; // lowercase

        // Act
        var result = _service.ReplaceMergeFields(template, person);

        // Assert - should NOT replace because field names are case-sensitive in the replacement regex
        result.Should().Be("Hello {{firstname}}!");
    }

    #endregion

    #region ValidateMergeFields Tests

    [Fact]
    public void ValidateMergeFields_WithValidFields_ReturnsSuccess()
    {
        // Arrange
        var text = "Hello {{FirstName}} {{LastName}}, your email is {{Email}}.";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ValidateMergeFields_WithAllValidFields_ReturnsSuccess()
    {
        // Arrange
        var text = "{{FirstName}} {{LastName}} {{NickName}} {{FullName}} {{Email}}";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMergeFields_WithUnknownField_ReturnsFailure()
    {
        // Arrange
        var text = "Hello {{UnknownField}}!";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("INVALID_MERGE_FIELDS");
        result.Error.Message.Should().Contain("UnknownField");
    }

    [Fact]
    public void ValidateMergeFields_WithMultipleUnknownFields_ListsAllInvalid()
    {
        // Arrange
        var text = "{{Unknown1}} and {{Unknown2}}";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("{{Unknown1}}");
        result.Error.Message.Should().Contain("{{Unknown2}}");
    }

    [Fact]
    public void ValidateMergeFields_WithDuplicateUnknownField_ListsOnce()
    {
        // Arrange
        var text = "{{Unknown}} {{Unknown}} {{Unknown}}";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Should only list {{Unknown}} once due to .Distinct()
        var occurrences = result.Error!.Message.Split("{{Unknown}}").Length - 1;
        occurrences.Should().Be(1);
    }

    [Fact]
    public void ValidateMergeFields_WithMixedValidAndInvalid_ReturnsFailure()
    {
        // Arrange
        var text = "Hello {{FirstName}}, your {{InvalidField}} is waiting.";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("{{InvalidField}}");
        // Error message should list valid fields to help users, so {{FirstName}} will appear in the "Valid fields are:" section
        result.Error.Message.Should().Contain("Valid fields are:");
    }

    [Fact]
    public void ValidateMergeFields_WithEmptyText_ReturnsSuccess()
    {
        // Arrange
        var text = "";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMergeFields_WithNullText_ReturnsSuccess()
    {
        // Arrange
        string? text = null;

        // Act
        var result = _service.ValidateMergeFields(text!);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMergeFields_WithNoMergeFields_ReturnsSuccess()
    {
        // Arrange
        var text = "This is plain text with no merge fields.";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateMergeFields_ErrorMessage_IncludesValidFieldsList()
    {
        // Arrange
        var text = "Hello {{InvalidField}}!";

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert
        result.Error!.Message.Should().Contain("{{FirstName}}");
        result.Error.Message.Should().Contain("{{LastName}}");
        result.Error.Message.Should().Contain("{{Email}}");
        result.Error.Message.Should().Contain("{{NickName}}");
        result.Error.Message.Should().Contain("{{FullName}}");
    }

    [Fact]
    public void ValidateMergeFields_IsCaseInsensitive_ForValidation()
    {
        // Arrange
        var text = "Hello {{firstname}}!"; // lowercase

        // Act
        var result = _service.ValidateMergeFields(text);

        // Assert - validation should be case-insensitive
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
