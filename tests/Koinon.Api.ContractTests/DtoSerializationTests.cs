using System.Text.Json;
using FluentAssertions;
using Koinon.Application.DTOs;
using Koinon.Domain.Enums;
using Xunit;

namespace Koinon.Api.ContractTests;

/// <summary>
/// Tests DTO serialization/deserialization contracts.
/// Ensures DTOs can be reliably serialized to/from JSON.
/// </summary>
public class DtoSerializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void PersonDto_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new PersonDto
        {
            IdKey = "abc123",
            Guid = Guid.NewGuid(),
            FirstName = "John",
            NickName = "Johnny",
            MiddleName = "Q",
            LastName = "Doe",
            FullName = "John Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            Age = 33,
            Gender = "Male",
            Email = "john@example.com",
            IsEmailActive = true,
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow,
            ModifiedDateTime = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<PersonDto>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IdKey.Should().Be(original.IdKey);
        deserialized.FirstName.Should().Be(original.FirstName);
        deserialized.LastName.Should().Be(original.LastName);
        deserialized.Email.Should().Be(original.Email);
        deserialized.BirthDate.Should().Be(original.BirthDate);
        deserialized.Age.Should().Be(original.Age);
    }

    [Fact]
    public void FamilyDto_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new FamilyDto
        {
            IdKey = "fam123",
            Guid = Guid.NewGuid(),
            Name = "Doe Family",
            Description = "Test family",
            IsActive = true,
            Members = new List<FamilyMemberDto>(),
            CreatedDateTime = DateTime.UtcNow,
            ModifiedDateTime = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<FamilyDto>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IdKey.Should().Be(original.IdKey);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Description.Should().Be(original.Description);
        deserialized.IsActive.Should().Be(original.IsActive);
    }

    [Fact]
    public void GroupDto_RoundTrip_PreservesData()
    {
        // Arrange
        var groupType = new GroupTypeSummaryDto
        {
            IdKey = "gt123",
            Guid = Guid.NewGuid(),
            Name = "Small Group",
            IsFamilyGroupType = false,
            AllowMultipleLocations = true,
            Roles = new List<GroupTypeRoleDto>()
        };

        var original = new GroupDto
        {
            IdKey = "grp123",
            Guid = Guid.NewGuid(),
            Name = "Youth Group",
            Description = "Test group",
            IsActive = true,
            IsArchived = false,
            IsSecurityRole = false,
            IsPublic = true,
            AllowGuests = true,
            Order = 0,
            GroupType = groupType,
            Members = new List<GroupMemberDto>(),
            ChildGroups = new List<GroupSummaryDto>(),
            CreatedDateTime = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<GroupDto>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IdKey.Should().Be(original.IdKey);
        deserialized.Name.Should().Be(original.Name);
        deserialized.IsActive.Should().Be(original.IsActive);
        deserialized.GroupType.Name.Should().Be(groupType.Name);
    }

    [Fact]
    public void AttendanceSummaryDto_RoundTrip_PreservesData()
    {
        // Arrange
        var person = new CheckinPersonSummaryDto(
            "per123",
            "John Doe",
            "John",
            "Doe",
            "Johnny",
            10,
            null
        );

        var location = new CheckinLocationSummaryDto(
            "loc123",
            "Kids Room",
            "Main Campus > Kids > Room 101"
        );

        var original = new AttendanceSummaryDto(
            "att123",
            person,
            location,
            DateTime.UtcNow,
            null,
            "ABC123",
            false,
            "Test note"
        );

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<AttendanceSummaryDto>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IdKey.Should().Be(original.IdKey);
        deserialized.Person.FullName.Should().Be(person.FullName);
        deserialized.Location.Name.Should().Be(location.Name);
        deserialized.SecurityCode.Should().Be(original.SecurityCode);
    }

    [Fact]
    public void LabelDto_RoundTrip_PreservesData()
    {
        // Arrange
        var fields = new Dictionary<string, string>
        {
            { "Name", "John Doe" },
            { "SecurityCode", "ABC123" },
            { "Room", "Kids Room" }
        };

        var original = new LabelDto(
            LabelType.ChildName,
            "Test label content",
            "ZPL",
            fields
        );

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<LabelDto>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().Be(original.Type);
        deserialized.Content.Should().Be(original.Content);
        deserialized.Format.Should().Be(original.Format);
        deserialized.Fields.Should().ContainKey("Name");
        deserialized.Fields["Name"].Should().Be("John Doe");
    }

    [Fact]
    public void DateTime_Serialization_UsesIso8601()
    {
        // Arrange
        var dto = new PersonDto
        {
            IdKey = "test",
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);

        // Assert
        json.Should().Contain("2024-01-15T10:30:45");
    }

    [Fact]
    public void DateOnly_Serialization_PreservesDate()
    {
        // Arrange
        var dto = new PersonDto
        {
            IdKey = "test",
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            BirthDate = new DateOnly(1990, 5, 15),
            CreatedDateTime = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);
        var deserialized = JsonSerializer.Deserialize<PersonDto>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.BirthDate.Should().Be(new DateOnly(1990, 5, 15));
    }

    [Fact]
    public void RequiredProperties_NotNull_AfterDeserialization()
    {
        // Arrange
        var json = @"{
            ""idKey"": ""test123"",
            ""guid"": ""550e8400-e29b-41d4-a716-446655440000"",
            ""firstName"": ""John"",
            ""lastName"": ""Doe"",
            ""fullName"": ""John Doe"",
            ""gender"": ""Male"",
            ""emailPreference"": ""EmailAllowed"",
            ""phoneNumbers"": [],
            ""isEmailActive"": true,
            ""createdDateTime"": ""2024-01-15T10:30:45Z""
        }";

        // Act
        var deserialized = JsonSerializer.Deserialize<PersonDto>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IdKey.Should().NotBeNullOrEmpty();
        deserialized.FirstName.Should().NotBeNullOrEmpty();
        deserialized.LastName.Should().NotBeNullOrEmpty();
        deserialized.FullName.Should().NotBeNullOrEmpty();
        deserialized.PhoneNumbers.Should().NotBeNull();
    }

    [Fact]
    public void CamelCase_NamingPolicy_Applied()
    {
        // Arrange
        var dto = new PersonDto
        {
            IdKey = "test",
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);

        // Assert
        json.Should().Contain("\"idKey\":");
        json.Should().Contain("\"firstName\":");
        json.Should().Contain("\"lastName\":");
        json.Should().NotContain("\"IdKey\":");
        json.Should().NotContain("\"FirstName\":");
    }
}
