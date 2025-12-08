using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class FollowUpControllerTests
{
    private readonly Mock<IFollowUpService> _followUpServiceMock;
    private readonly Mock<ILogger<FollowUpController>> _loggerMock;
    private readonly FollowUpController _controller;

    // Valid IdKeys for testing
    private readonly string _followUpIdKey = IdKeyHelper.Encode(123);
    private readonly string _personIdKey = IdKeyHelper.Encode(456);
    private readonly string _assignedToIdKey = IdKeyHelper.Encode(789);

    public FollowUpControllerTests()
    {
        _followUpServiceMock = new Mock<IFollowUpService>();
        _loggerMock = new Mock<ILogger<FollowUpController>>();
        _controller = new FollowUpController(_followUpServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetPending Tests

    [Fact]
    public async Task GetPending_WithoutFilter_ReturnsAllPendingFollowUps()
    {
        // Arrange
        var expectedFollowUps = new List<FollowUpDto>
        {
            new()
            {
                IdKey = _followUpIdKey,
                PersonIdKey = _personIdKey,
                PersonName = "John Doe",
                AttendanceIdKey = null,
                Status = FollowUpStatus.Pending,
                Notes = null,
                AssignedToIdKey = null,
                AssignedToName = null,
                ContactedDateTime = null,
                CompletedDateTime = null,
                CreatedDateTime = DateTime.UtcNow
            }
        };

        _followUpServiceMock
            .Setup(s => s.GetPendingFollowUpsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFollowUps);

        // Act
        var result = await _controller.GetPending();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var followUps = okResult.Value.Should().BeAssignableTo<IReadOnlyList<FollowUpDto>>().Subject;
        followUps.Should().HaveCount(1);
        followUps[0].Status.Should().Be(FollowUpStatus.Pending);
    }

    [Fact]
    public async Task GetPending_WithAssignedToFilter_ReturnsFilteredFollowUps()
    {
        // Arrange
        var expectedFollowUps = new List<FollowUpDto>
        {
            new()
            {
                IdKey = _followUpIdKey,
                PersonIdKey = _personIdKey,
                PersonName = "John Doe",
                AttendanceIdKey = null,
                Status = FollowUpStatus.Pending,
                Notes = null,
                AssignedToIdKey = _assignedToIdKey,
                AssignedToName = "Jane Smith",
                ContactedDateTime = null,
                CompletedDateTime = null,
                CreatedDateTime = DateTime.UtcNow
            }
        };

        _followUpServiceMock
            .Setup(s => s.GetPendingFollowUpsAsync(_assignedToIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFollowUps);

        // Act
        var result = await _controller.GetPending(_assignedToIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var followUps = okResult.Value.Should().BeAssignableTo<IReadOnlyList<FollowUpDto>>().Subject;
        followUps.Should().HaveCount(1);
        followUps[0].AssignedToIdKey.Should().Be(_assignedToIdKey);
    }

    [Fact]
    public async Task GetPending_WithInvalidAssignedToIdKey_ReturnsBadRequest()
    {
        // Arrange
        var invalidIdKey = "invalid-idkey-123";

        // Act
        var result = await _controller.GetPending(invalidIdKey);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Invalid IdKey");
        problemDetails.Detail.Should().Contain(invalidIdKey);
    }

    #endregion

    #region GetByIdKey Tests

    [Fact]
    public async Task GetByIdKey_WhenFollowUpExists_ReturnsOkWithFollowUp()
    {
        // Arrange
        var expectedFollowUp = new FollowUpDto
        {
            IdKey = _followUpIdKey,
            PersonIdKey = _personIdKey,
            PersonName = "John Doe",
            AttendanceIdKey = null,
            Status = FollowUpStatus.Pending,
            Notes = "Test follow-up",
            AssignedToIdKey = null,
            AssignedToName = null,
            ContactedDateTime = null,
            CompletedDateTime = null,
            CreatedDateTime = DateTime.UtcNow
        };

        _followUpServiceMock
            .Setup(s => s.GetByIdKeyAsync(_followUpIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFollowUp);

        // Act
        var result = await _controller.GetByIdKey(_followUpIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var followUp = okResult.Value.Should().BeOfType<FollowUpDto>().Subject;
        followUp.IdKey.Should().Be(_followUpIdKey);
        followUp.PersonName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetByIdKey_WhenFollowUpDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _followUpServiceMock
            .Setup(s => s.GetByIdKeyAsync(_followUpIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FollowUpDto?)null);

        // Act
        var result = await _controller.GetByIdKey(_followUpIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Follow-up not found");
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public async Task UpdateStatus_WhenSuccessful_ReturnsOkWithUpdatedFollowUp()
    {
        // Arrange
        var request = new UpdateFollowUpStatusRequest
        {
            Status = FollowUpStatus.Contacted,
            Notes = "Successfully contacted"
        };

        var updatedFollowUp = new FollowUpDto
        {
            IdKey = _followUpIdKey,
            PersonIdKey = _personIdKey,
            PersonName = "John Doe",
            AttendanceIdKey = null,
            Status = FollowUpStatus.Contacted,
            Notes = "Successfully contacted",
            AssignedToIdKey = null,
            AssignedToName = null,
            ContactedDateTime = DateTime.UtcNow,
            CompletedDateTime = null,
            CreatedDateTime = DateTime.UtcNow.AddDays(-1)
        };

        _followUpServiceMock
            .Setup(s => s.UpdateStatusAsync(
                _followUpIdKey,
                FollowUpStatus.Contacted,
                "Successfully contacted",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FollowUpDto>.Success(updatedFollowUp));

        // Act
        var result = await _controller.UpdateStatus(_followUpIdKey, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var followUp = okResult.Value.Should().BeOfType<FollowUpDto>().Subject;
        followUp.Status.Should().Be(FollowUpStatus.Contacted);
        followUp.Notes.Should().Be("Successfully contacted");
    }

    [Fact]
    public async Task UpdateStatus_WhenFollowUpNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateFollowUpStatusRequest
        {
            Status = FollowUpStatus.Contacted,
            Notes = "Test"
        };

        var error = new Error("NOT_FOUND", "Follow-up not found");

        _followUpServiceMock
            .Setup(s => s.UpdateStatusAsync(
                _followUpIdKey,
                It.IsAny<FollowUpStatus>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FollowUpDto>.Failure(error));

        // Act
        var result = await _controller.UpdateStatus(_followUpIdKey, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(404);
    }

    [Fact]
    public async Task UpdateStatus_WhenValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateFollowUpStatusRequest
        {
            Status = FollowUpStatus.Contacted,
            Notes = null
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "Validation failed",
            new Dictionary<string, string[]>
            {
                { "Notes", new[] { "Notes are required when status is Contacted" } }
            });

        _followUpServiceMock
            .Setup(s => s.UpdateStatusAsync(
                _followUpIdKey,
                It.IsAny<FollowUpStatus>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FollowUpDto>.Failure(error));

        // Act
        var result = await _controller.UpdateStatus(_followUpIdKey, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(400);
    }

    #endregion

    #region Assign Tests

    [Fact]
    public async Task Assign_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var request = new AssignFollowUpRequest
        {
            AssignedToIdKey = _assignedToIdKey
        };

        _followUpServiceMock
            .Setup(s => s.AssignFollowUpAsync(
                _followUpIdKey,
                _assignedToIdKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Assign(_followUpIdKey, request);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Assign_WhenFollowUpNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AssignFollowUpRequest
        {
            AssignedToIdKey = _assignedToIdKey
        };

        var error = new Error("NOT_FOUND", "Follow-up not found");

        _followUpServiceMock
            .Setup(s => s.AssignFollowUpAsync(
                _followUpIdKey,
                _assignedToIdKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Assign(_followUpIdKey, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(404);
    }

    [Fact]
    public async Task Assign_WhenPersonNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AssignFollowUpRequest
        {
            AssignedToIdKey = _assignedToIdKey
        };

        var error = new Error("NOT_FOUND", "Person to assign not found");

        _followUpServiceMock
            .Setup(s => s.AssignFollowUpAsync(
                _followUpIdKey,
                _assignedToIdKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Assign(_followUpIdKey, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(404);
        problemDetails.Detail.Should().Be("Person to assign not found");
    }

    [Fact]
    public async Task Assign_WithInvalidAssignedToIdKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new AssignFollowUpRequest
        {
            AssignedToIdKey = "invalid-idkey-xyz"
        };

        // Act
        var result = await _controller.Assign(_followUpIdKey, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Invalid IdKey");
        problemDetails.Detail.Should().Contain("invalid-idkey-xyz");

        // Verify service was never called
        _followUpServiceMock.Verify(
            s => s.AssignFollowUpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
