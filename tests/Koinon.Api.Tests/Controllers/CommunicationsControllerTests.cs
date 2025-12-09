using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Communication;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class CommunicationsControllerTests
{
    private readonly Mock<ICommunicationService> _communicationServiceMock;
    private readonly Mock<ICommunicationAnalyticsService> _analyticsServiceMock;
    private readonly Mock<ILogger<CommunicationsController>> _loggerMock;
    private readonly CommunicationsController _controller;

    public CommunicationsControllerTests()
    {
        _communicationServiceMock = new Mock<ICommunicationService>();
        _analyticsServiceMock = new Mock<ICommunicationAnalyticsService>();
        _loggerMock = new Mock<ILogger<CommunicationsController>>();
        _controller = new CommunicationsController(
            _communicationServiceMock.Object,
            _analyticsServiceMock.Object,
            _loggerMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedResult()
    {
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "test@example.com",
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var communicationDto = new CommunicationDto
        {
            IdKey = "XYZ789",
            Guid = Guid.NewGuid(),
            CommunicationType = "Email",
            Status = "Draft",
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 1,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedDateTime = DateTime.UtcNow,
            Recipients = new List<CommunicationRecipientDto>()
        };
        _communicationServiceMock
            .Setup(x => x.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CommunicationDto>.Success(communicationDto));
        var result = await _controller.Create(dto);
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(CommunicationsController.GetByIdKey));
        createdResult.Value.Should().Be(communicationDto);
    }

    [Fact]
    public async Task Create_WithValidationError_ReturnsBadRequest()
    {
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "test@example.com",
            GroupIdKeys = new List<string> { "ABC123" }
        };
        var error = new Error("VALIDATION_ERROR", "Validation failed");
        _communicationServiceMock
            .Setup(x => x.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CommunicationDto>.Failure(error));
        var result = await _controller.Create(dto);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByIdKey_WithExistingId_ReturnsOkResult()
    {
        var idKey = "ABC123";
        var communicationDto = new CommunicationDto
        {
            IdKey = idKey,
            Guid = Guid.NewGuid(),
            CommunicationType = "Email",
            Status = "Draft",
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 1,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedDateTime = DateTime.UtcNow,
            Recipients = new List<CommunicationRecipientDto>()
        };
        _communicationServiceMock
            .Setup(x => x.GetByIdKeyAsync(idKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(communicationDto);
        var result = await _controller.GetByIdKey(idKey);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(communicationDto);
    }

    [Fact]
    public async Task GetByIdKey_WithNonExistingId_ReturnsNotFound()
    {
        var idKey = "NONEXISTENT";
        _communicationServiceMock
            .Setup(x => x.GetByIdKeyAsync(idKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommunicationDto?)null);
        var result = await _controller.GetByIdKey(idKey);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Search_WithValidParameters_ReturnsOkResult()
    {
        var pagedResult = new PagedResult<CommunicationSummaryDto>(
            new List<CommunicationSummaryDto>(),
            0,
            1,
            20);
        _communicationServiceMock
            .Setup(x => x.SearchAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);
        var result = await _controller.Search(page: 1, pageSize: 20);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task Delete_WithExistingDraft_ReturnsNoContent()
    {
        var idKey = "ABC123";
        _communicationServiceMock
            .Setup(x => x.DeleteAsync(idKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var result = await _controller.Delete(idKey);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExisting_ReturnsNotFound()
    {
        var idKey = "NONEXISTENT";
        var error = new Error("NOT_FOUND", "Communication not found");
        _communicationServiceMock
            .Setup(x => x.DeleteAsync(idKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));
        var result = await _controller.Delete(idKey);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Send_WithExistingDraft_ReturnsOkResult()
    {
        var idKey = "ABC123";
        var communicationDto = new CommunicationDto
        {
            IdKey = idKey,
            Guid = Guid.NewGuid(),
            CommunicationType = "Email",
            Status = "Pending",
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 1,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedDateTime = DateTime.UtcNow,
            Recipients = new List<CommunicationRecipientDto>()
        };
        _communicationServiceMock
            .Setup(x => x.SendAsync(idKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CommunicationDto>.Success(communicationDto));
        var result = await _controller.Send(idKey);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(communicationDto);
    }
}
