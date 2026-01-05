using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class CampusesControllerTests
{
    private readonly Mock<ICampusService> _campusServiceMock;
    private readonly Mock<ILogger<CampusesController>> _loggerMock;
    private readonly CampusesController _controller;

    private readonly string _campusIdKey = IdKeyHelper.Encode(1);

    public CampusesControllerTests()
    {
        _campusServiceMock = new Mock<ICampusService>();
        _loggerMock = new Mock<ILogger<CampusesController>>();
        _controller = new CampusesController(_campusServiceMock.Object, _loggerMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCampuses()
    {
        // Arrange
        var expectedCampuses = new List<CampusSummaryDto>
        {
            new() { IdKey = _campusIdKey, Name = "Main Campus", ShortCode = "MAIN" }
        };

        _campusServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCampuses);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var campuses = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<CampusSummaryDto>>().Subject;
        campuses.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdKey_WithExistingCampus_ReturnsOkWithCampus()
    {
        // Arrange
        var expectedCampus = new CampusDto
        {
            IdKey = _campusIdKey,
            Guid = Guid.NewGuid(),
            Name = "Main Campus",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        _campusServiceMock
            .Setup(s => s.GetByIdKeyAsync(_campusIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CampusDto>.Success(expectedCampus));

        // Act
        var result = await _controller.GetByIdKey(_campusIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var campus = dataProperty!.GetValue(response).Should().BeOfType<CampusDto>().Subject;
        campus.IdKey.Should().Be(_campusIdKey);
    }

    [Fact]
    public async Task GetByIdKey_WithNonExistentCampus_ReturnsNotFound()
    {
        // Arrange
        _campusServiceMock
            .Setup(s => s.GetByIdKeyAsync(_campusIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CampusDto>.Failure(Error.NotFound("Campus", _campusIdKey)));

        // Act
        var result = await _controller.GetByIdKey(_campusIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedWithCampus()
    {
        // Arrange
        var request = new CreateCampusRequest { Name = "New Campus" };
        var createdCampus = new CampusDto
        {
            IdKey = _campusIdKey,
            Guid = Guid.NewGuid(),
            Name = "New Campus",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow
        };

        _campusServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CampusDto>.Success(createdCampus));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(CampusesController.GetByIdKey));

        var response = createdResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var campus = dataProperty!.GetValue(response).Should().BeOfType<CampusDto>().Subject;
        campus.Name.Should().Be("New Campus");
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOkWithUpdatedCampus()
    {
        // Arrange
        var request = new UpdateCampusRequest { Name = "Updated Campus" };
        var updatedCampus = new CampusDto
        {
            IdKey = _campusIdKey,
            Guid = Guid.NewGuid(),
            Name = "Updated Campus",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedDateTime = DateTime.UtcNow
        };

        _campusServiceMock
            .Setup(s => s.UpdateAsync(_campusIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CampusDto>.Success(updatedCampus));

        // Act
        var result = await _controller.Update(_campusIdKey, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var campus = dataProperty!.GetValue(response).Should().BeOfType<CampusDto>().Subject;
        campus.Name.Should().Be("Updated Campus");
    }

    [Fact]
    public async Task Delete_WithExistingCampus_ReturnsNoContent()
    {
        // Arrange
        _campusServiceMock
            .Setup(s => s.DeleteAsync(_campusIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete(_campusIdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
