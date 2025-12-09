using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the GroupMemberRequest entity.
/// </summary>
public class GroupMemberRequestTests
{
    [Fact]
    public void GroupMemberRequest_Creation_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2
        };

        // Assert
        Assert.Equal(1, request.GroupId);
        Assert.Equal(2, request.PersonId);
        Assert.NotEqual(Guid.Empty, request.Guid); // Should auto-generate
    }

    [Fact]
    public void Status_DefaultValue_ShouldBePending()
    {
        // Arrange & Act
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2
        };

        // Assert
        Assert.Equal(GroupMemberRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Status_CanBeSet_ToApproved()
    {
        // Arrange
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            Status = GroupMemberRequestStatus.Approved
        };

        // Act & Assert
        Assert.Equal(GroupMemberRequestStatus.Approved, request.Status);
    }

    [Fact]
    public void Status_CanBeSet_ToDenied()
    {
        // Arrange
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            Status = GroupMemberRequestStatus.Denied
        };

        // Act & Assert
        Assert.Equal(GroupMemberRequestStatus.Denied, request.Status);
    }

    [Fact]
    public void RequestNote_CanBeSet()
    {
        // Arrange
        var note = "I would like to join this group to serve in children's ministry.";
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            RequestNote = note
        };

        // Act & Assert
        Assert.Equal(note, request.RequestNote);
    }

    [Fact]
    public void RequestNote_CanBeNull()
    {
        // Arrange & Act
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2
        };

        // Assert
        Assert.Null(request.RequestNote);
    }

    [Fact]
    public void ResponseNote_CanBeSet()
    {
        // Arrange
        var note = "Approved after background check completion.";
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            Status = GroupMemberRequestStatus.Approved,
            ResponseNote = note
        };

        // Act & Assert
        Assert.Equal(note, request.ResponseNote);
    }

    [Fact]
    public void ResponseNote_CanBeNull()
    {
        // Arrange & Act
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2
        };

        // Assert
        Assert.Null(request.ResponseNote);
    }

    [Fact]
    public void ProcessedByPersonId_CanBeSet()
    {
        // Arrange
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            Status = GroupMemberRequestStatus.Approved,
            ProcessedByPersonId = 999
        };

        // Act & Assert
        Assert.Equal(999, request.ProcessedByPersonId);
    }

    [Fact]
    public void ProcessedByPersonId_CanBeNull()
    {
        // Arrange & Act
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2
        };

        // Assert
        Assert.Null(request.ProcessedByPersonId);
    }

    [Fact]
    public void ProcessedDateTime_CanBeSet()
    {
        // Arrange
        var processedDate = new DateTime(2025, 12, 8, 14, 30, 0);
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            Status = GroupMemberRequestStatus.Approved,
            ProcessedDateTime = processedDate
        };

        // Act & Assert
        Assert.Equal(processedDate, request.ProcessedDateTime);
    }

    [Fact]
    public void ProcessedDateTime_CanBeNull()
    {
        // Arrange & Act
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2
        };

        // Assert
        Assert.Null(request.ProcessedDateTime);
    }

    [Fact]
    public void IdKey_ShouldBeGenerated_FromId()
    {
        // Arrange
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            Id = 456
        };

        // Act
        var idKey = request.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
    }

    [Fact]
    public void NavigationProperties_CanBeSet()
    {
        // Arrange
        var person = new Person { FirstName = "Jane", LastName = "Smith", Id = 2 };
        var group = new Group { Name = "Volunteer Team", GroupTypeId = 1, Id = 1 };
        var processor = new Person { FirstName = "John", LastName = "Leader", Id = 999 };

        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            ProcessedByPersonId = 999,
            Person = person,
            Group = group,
            ProcessedByPerson = processor
        };

        // Act & Assert
        Assert.Equal(person, request.Person);
        Assert.Equal(group, request.Group);
        Assert.Equal(processor, request.ProcessedByPerson);
    }

    [Fact]
    public void ApprovedRequest_ShouldHaveCompleteWorkflow()
    {
        // Arrange
        var requestDate = new DateTime(2025, 12, 1, 9, 0, 0);
        var processedDate = new DateTime(2025, 12, 8, 14, 30, 0);

        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            RequestNote = "I want to volunteer with the children's ministry.",
            CreatedDateTime = requestDate,
            Status = GroupMemberRequestStatus.Approved,
            ResponseNote = "Welcome to the team!",
            ProcessedByPersonId = 999,
            ProcessedDateTime = processedDate
        };

        // Act & Assert
        Assert.Equal(GroupMemberRequestStatus.Approved, request.Status);
        Assert.NotNull(request.RequestNote);
        Assert.NotNull(request.ResponseNote);
        Assert.NotNull(request.ProcessedByPersonId);
        Assert.NotNull(request.ProcessedDateTime);
        Assert.True(request.ProcessedDateTime > request.CreatedDateTime);
    }

    [Fact]
    public void DeniedRequest_ShouldHaveCompleteWorkflow()
    {
        // Arrange
        var requestDate = new DateTime(2025, 12, 1, 9, 0, 0);
        var processedDate = new DateTime(2025, 12, 8, 14, 30, 0);

        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            RequestNote = "I want to join this group.",
            CreatedDateTime = requestDate,
            Status = GroupMemberRequestStatus.Denied,
            ResponseNote = "Group is at capacity.",
            ProcessedByPersonId = 999,
            ProcessedDateTime = processedDate
        };

        // Act & Assert
        Assert.Equal(GroupMemberRequestStatus.Denied, request.Status);
        Assert.NotNull(request.RequestNote);
        Assert.NotNull(request.ResponseNote);
        Assert.NotNull(request.ProcessedByPersonId);
        Assert.NotNull(request.ProcessedDateTime);
    }

    [Fact]
    public void PendingRequest_ShouldNotHaveProcessedData()
    {
        // Arrange & Act
        var request = new GroupMemberRequest
        {
            GroupId = 1,
            PersonId = 2,
            RequestNote = "I want to join this group.",
            Status = GroupMemberRequestStatus.Pending
        };

        // Assert
        Assert.Equal(GroupMemberRequestStatus.Pending, request.Status);
        Assert.Null(request.ResponseNote);
        Assert.Null(request.ProcessedByPersonId);
        Assert.Null(request.ProcessedDateTime);
    }
}
