using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

/// <summary>
/// Tests for contribution statement endpoints in GivingController.
/// </summary>
public class ContributionStatementsControllerTests
{
    private readonly Mock<IBatchDonationEntryService> _batchDonationServiceMock;
    private readonly Mock<IContributionStatementService> _statementServiceMock;
    private readonly Mock<ILogger<GivingController>> _loggerMock;
    private readonly GivingController _controller;

    // Valid IdKeys for testing
    private readonly string _statementIdKey = IdKeyHelper.Encode(100);
    private readonly string _personIdKey = IdKeyHelper.Encode(200);

    public ContributionStatementsControllerTests()
    {
        _batchDonationServiceMock = new Mock<IBatchDonationEntryService>();
        _statementServiceMock = new Mock<IContributionStatementService>();
        _loggerMock = new Mock<ILogger<GivingController>>();
        _controller = new GivingController(
            _batchDonationServiceMock.Object,
            _statementServiceMock.Object,
            _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetStatementsAsync Tests

    [Fact]
    public async Task GetStatementsAsync_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var statements = new List<ContributionStatementDto>
        {
            new()
            {
                IdKey = _statementIdKey,
                PersonIdKey = _personIdKey,
                PersonName = "John Doe",
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                TotalAmount = 5000.00m,
                ContributionCount = 12,
                GeneratedDateTime = DateTime.UtcNow
            }
        };

        var pagedResult = new PagedResult<ContributionStatementDto>(statements, 1, 1, 25);
        _statementServiceMock
            .Setup(s => s.GetStatementsAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<ContributionStatementDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetStatementsAsync(1, 25);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ContributionStatementDto>>().Subject.ToList();
        items.Should().HaveCount(1);
        items[0].IdKey.Should().Be(_statementIdKey);
    }

    [Fact]
    public async Task GetStatementsAsync_WithInvalidPage_NormalizesToOne()
    {
        // Arrange
        var pagedResult = new PagedResult<ContributionStatementDto>(new List<ContributionStatementDto>(), 0, 1, 25);
        _statementServiceMock
            .Setup(s => s.GetStatementsAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<ContributionStatementDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetStatementsAsync(-1, 25);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _statementServiceMock.Verify(s => s.GetStatementsAsync(1, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatementsAsync_WithInvalidPageSize_NormalizesToDefault()
    {
        // Arrange
        var pagedResult = new PagedResult<ContributionStatementDto>(new List<ContributionStatementDto>(), 0, 1, 25);
        _statementServiceMock
            .Setup(s => s.GetStatementsAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<ContributionStatementDto>>.Success(pagedResult));

        // Act
        var result = await _controller.GetStatementsAsync(1, 200);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _statementServiceMock.Verify(s => s.GetStatementsAsync(1, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatementsAsync_ServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        _statementServiceMock
            .Setup(s => s.GetStatementsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<ContributionStatementDto>>.Failure(new Error("SERVICE_ERROR", "Service failed")));

        // Act
        var result = await _controller.GetStatementsAsync(1, 25);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region GetStatementAsync Tests

    [Fact]
    public async Task GetStatementAsync_WithValidIdKey_ReturnsOkWithStatement()
    {
        // Arrange
        var statement = new ContributionStatementDto
        {
            IdKey = _statementIdKey,
            PersonIdKey = _personIdKey,
            PersonName = "John Doe",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            TotalAmount = 5000.00m,
            ContributionCount = 12,
            GeneratedDateTime = DateTime.UtcNow
        };

        _statementServiceMock
            .Setup(s => s.GetStatementAsync(_statementIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ContributionStatementDto>.Success(statement));

        // Act
        var result = await _controller.GetStatementAsync(_statementIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var statementDto = dataProperty!.GetValue(response).Should().BeOfType<ContributionStatementDto>().Subject;
        statementDto.IdKey.Should().Be(_statementIdKey);
    }

    [Fact]
    public async Task GetStatementAsync_WithNonExistentIdKey_ReturnsNotFound()
    {
        // Arrange
        _statementServiceMock
            .Setup(s => s.GetStatementAsync(_statementIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ContributionStatementDto>.Failure(new Error("NOT_FOUND", "Statement not found")));

        // Act
        var result = await _controller.GetStatementAsync(_statementIdKey);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion

    #region PreviewStatementAsync Tests

    [Fact]
    public async Task PreviewStatementAsync_WithValidRequest_ReturnsOkWithPreview()
    {
        // Arrange
        var request = new GenerateStatementRequest
        {
            PersonIdKey = _personIdKey,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };

        var preview = new StatementPreviewDto
        {
            PersonIdKey = _personIdKey,
            PersonName = "John Doe",
            PersonAddress = "123 Main St, City, ST 12345",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalAmount = 5000.00m,
            Contributions = new List<StatementContributionDto>
            {
                new() { Date = new DateTime(2024, 1, 15), FundName = "General Fund", Amount = 500.00m, CheckNumber = "1001" }
            },
            ChurchName = "First Church",
            ChurchAddress = "456 Church St, City, ST 12345"
        };

        _statementServiceMock
            .Setup(s => s.PreviewStatementAsync(It.IsAny<GenerateStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StatementPreviewDto>.Success(preview));

        // Act
        var result = await _controller.PreviewStatementAsync(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var previewDto = dataProperty!.GetValue(response).Should().BeOfType<StatementPreviewDto>().Subject;
        previewDto.TotalAmount.Should().Be(5000.00m);
        previewDto.Contributions.Should().HaveCount(1);
    }

    [Fact]
    public async Task PreviewStatementAsync_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var request = new GenerateStatementRequest
        {
            PersonIdKey = _personIdKey,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };

        _statementServiceMock
            .Setup(s => s.PreviewStatementAsync(It.IsAny<GenerateStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StatementPreviewDto>.Failure(new Error("NOT_FOUND", "Person not found")));

        // Act
        var result = await _controller.PreviewStatementAsync(request);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task PreviewStatementAsync_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var request = new GenerateStatementRequest
        {
            PersonIdKey = _personIdKey,
            StartDate = new DateTime(2024, 12, 31),
            EndDate = new DateTime(2024, 1, 1)
        };

        _statementServiceMock
            .Setup(s => s.PreviewStatementAsync(It.IsAny<GenerateStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StatementPreviewDto>.Failure(new Error("VALIDATION_ERROR", "End date must be after start date")));

        // Act
        var result = await _controller.PreviewStatementAsync(request);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region GenerateStatementAsync Tests

    [Fact]
    public async Task GenerateStatementAsync_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new GenerateStatementRequest
        {
            PersonIdKey = _personIdKey,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };

        var statement = new ContributionStatementDto
        {
            IdKey = _statementIdKey,
            PersonIdKey = _personIdKey,
            PersonName = "John Doe",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalAmount = 5000.00m,
            ContributionCount = 12,
            GeneratedDateTime = DateTime.UtcNow
        };

        _statementServiceMock
            .Setup(s => s.GenerateStatementAsync(It.IsAny<GenerateStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ContributionStatementDto>.Success(statement));

        // Act
        var result = await _controller.GenerateStatementAsync(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(GivingController.GetStatementAsync));
        createdResult.RouteValues!["idKey"].Should().Be(_statementIdKey);
    }

    [Fact]
    public async Task GenerateStatementAsync_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var request = new GenerateStatementRequest
        {
            PersonIdKey = _personIdKey,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };

        _statementServiceMock
            .Setup(s => s.GenerateStatementAsync(It.IsAny<GenerateStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ContributionStatementDto>.Failure(new Error("NOT_FOUND", "Person not found")));

        // Act
        var result = await _controller.GenerateStatementAsync(request);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion

    #region GetStatementPdfAsync Tests

    [Fact]
    public async Task GetStatementPdfAsync_WithValidIdKey_ReturnsFile()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header

        _statementServiceMock
            .Setup(s => s.GenerateStatementPdfAsync(_statementIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<byte[]>.Success(pdfBytes));

        // Act
        var result = await _controller.GetStatementPdfAsync(_statementIdKey);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be($"contribution-statement-{_statementIdKey}.pdf");
        fileResult.FileContents.Should().BeEquivalentTo(pdfBytes);
    }

    [Fact]
    public async Task GetStatementPdfAsync_WithNonExistentIdKey_ReturnsNotFound()
    {
        // Arrange
        _statementServiceMock
            .Setup(s => s.GenerateStatementPdfAsync(_statementIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<byte[]>.Failure(new Error("NOT_FOUND", "Statement not found")));

        // Act
        var result = await _controller.GetStatementPdfAsync(_statementIdKey);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion

    #region GetEligiblePeopleAsync Tests

    [Fact]
    public async Task GetEligiblePeopleAsync_WithValidParameters_ReturnsOkWithList()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var minimumAmount = 100.00m;

        var eligiblePeople = new List<EligiblePersonDto>
        {
            new()
            {
                PersonIdKey = _personIdKey,
                PersonName = "John Doe",
                TotalAmount = 5000.00m,
                ContributionCount = 12
            }
        };

        _statementServiceMock
            .Setup(s => s.GetEligiblePeopleAsync(It.IsAny<BatchStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<EligiblePersonDto>>.Success(eligiblePeople));

        // Act
        var result = await _controller.GetEligiblePeopleAsync(startDate, endDate, minimumAmount);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var people = dataProperty!.GetValue(response).Should().BeAssignableTo<List<EligiblePersonDto>>().Subject;
        people.Should().HaveCount(1);
        people[0].PersonIdKey.Should().Be(_personIdKey);
    }

    [Fact]
    public async Task GetEligiblePeopleAsync_WithDefaultMinimumAmount_UsesZero()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        _statementServiceMock
            .Setup(s => s.GetEligiblePeopleAsync(It.IsAny<BatchStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<EligiblePersonDto>>.Success(new List<EligiblePersonDto>()));

        // Act
        var result = await _controller.GetEligiblePeopleAsync(startDate, endDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _statementServiceMock.Verify(
            s => s.GetEligiblePeopleAsync(
                It.Is<BatchStatementRequest>(r => r.MinimumAmount == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEligiblePeopleAsync_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var startDate = new DateTime(2024, 12, 31);
        var endDate = new DateTime(2024, 1, 1);

        _statementServiceMock
            .Setup(s => s.GetEligiblePeopleAsync(It.IsAny<BatchStatementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<EligiblePersonDto>>.Failure(new Error("VALIDATION_ERROR", "Invalid date range")));

        // Act
        var result = await _controller.GetEligiblePeopleAsync(startDate, endDate);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion
}
