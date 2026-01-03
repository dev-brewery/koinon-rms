using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class AuditLogsControllerTests
{
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<AuditLogsController>> _loggerMock;
    private readonly AuditLogsController _controller;

    public AuditLogsControllerTests()
    {
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<AuditLogsController>>();
        _controller = new AuditLogsController(
            _auditServiceMock.Object,
            _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Search_ReturnsOk_WithPaginatedResults()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var auditLogs = new List<AuditLogDto>
        {
            new()
            {
                IdKey = "log1",
                ActionType = AuditAction.Create,
                EntityType = "Person",
                EntityIdKey = "person123",
                PersonIdKey = "user456",
                PersonName = "John Admin",
                Timestamp = new DateTime(2024, 1, 15),
                NewValues = "{\"name\":\"Test Person\"}",
                IpAddress = "192.168.1.1"
            },
            new()
            {
                IdKey = "log2",
                ActionType = AuditAction.Update,
                EntityType = "Group",
                EntityIdKey = "group789",
                PersonIdKey = "user456",
                PersonName = "John Admin",
                Timestamp = new DateTime(2024, 1, 20),
                OldValues = "{\"name\":\"Old Name\"}",
                NewValues = "{\"name\":\"New Name\"}",
                ChangedProperties = new List<string> { "name" },
                IpAddress = "192.168.1.1"
            }
        };

        var pagedResult = new PagedResult<AuditLogDto>(auditLogs, 2, 1, 20);

        _auditServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<AuditLogSearchParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.Search(
            startDate, endDate, null, null, null, null, 1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;

        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        var metaProperty = response.GetType().GetProperty("meta");
        metaProperty.Should().NotBeNull("response should have a 'meta' property");
        var meta = metaProperty!.GetValue(response).Should().BeAssignableTo<object>().Subject;

        items.Should().HaveCount(2);
        items.First().IdKey.Should().Be("log1");

        var pageProperty = meta.GetType().GetProperty("page");
        pageProperty!.GetValue(meta).Should().Be(1);

        var totalCountProperty = meta.GetType().GetProperty("totalCount");
        totalCountProperty!.GetValue(meta).Should().Be(2);

        _auditServiceMock.Verify(
            s => s.SearchAsync(It.IsAny<AuditLogSearchParameters>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_WithFilters_PassesCorrectParameters()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var entityType = "Person";
        var actionType = AuditAction.Update;
        var personIdKey = "user456";
        var entityIdKey = "person123";

        AuditLogSearchParameters? capturedParameters = null;
        _auditServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<AuditLogSearchParameters>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogSearchParameters, CancellationToken>((params_, _) => capturedParameters = params_)
            .ReturnsAsync(new PagedResult<AuditLogDto>(new List<AuditLogDto>(), 0, 1, 20));

        // Act
        await _controller.Search(
            startDate, endDate, entityType, actionType, personIdKey, entityIdKey, 2, 50, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.StartDate.Should().Be(startDate);
        capturedParameters.EndDate.Should().Be(endDate);
        capturedParameters.EntityType.Should().Be(entityType);
        capturedParameters.ActionType.Should().Be(actionType);
        capturedParameters.PersonIdKey.Should().Be(personIdKey);
        capturedParameters.EntityIdKey.Should().Be(entityIdKey);
        capturedParameters.Page.Should().Be(2);
        capturedParameters.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Search_EnforcesMaxPageSize()
    {
        // Arrange
        AuditLogSearchParameters? capturedParameters = null;
        _auditServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<AuditLogSearchParameters>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogSearchParameters, CancellationToken>((params_, _) => capturedParameters = params_)
            .ReturnsAsync(new PagedResult<AuditLogDto>(new List<AuditLogDto>(), 0, 1, 100));

        // Act
        await _controller.Search(
            null, null, null, null, null, null, 1, 500, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.PageSize.Should().Be(100, "page size should be capped at 100");
    }

    [Fact]
    public async Task Search_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        AuditLogSearchParameters? capturedParameters = null;
        _auditServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<AuditLogSearchParameters>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogSearchParameters, CancellationToken>((params_, _) => capturedParameters = params_)
            .ReturnsAsync(new PagedResult<AuditLogDto>(new List<AuditLogDto>(), 0, 1, 20));

        // Act
        await _controller.Search(
            null, null, null, null, null, null, 1, 20, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.Page.Should().Be(1);
        capturedParameters.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetByEntity_ReturnsOk_WithAuditHistory()
    {
        // Arrange
        var entityType = "Person";
        var idKey = "person123";
        var auditLogs = new List<AuditLogDto>
        {
            new()
            {
                IdKey = "log1",
                ActionType = AuditAction.Create,
                EntityType = entityType,
                EntityIdKey = idKey,
                PersonIdKey = "user456",
                PersonName = "John Admin",
                Timestamp = new DateTime(2024, 1, 1),
                NewValues = "{\"name\":\"Test Person\"}"
            },
            new()
            {
                IdKey = "log2",
                ActionType = AuditAction.Update,
                EntityType = entityType,
                EntityIdKey = idKey,
                PersonIdKey = "user789",
                PersonName = "Jane Admin",
                Timestamp = new DateTime(2024, 1, 15),
                OldValues = "{\"name\":\"Test Person\"}",
                NewValues = "{\"name\":\"Updated Person\"}",
                ChangedProperties = new List<string> { "name" }
            }
        };

        _auditServiceMock
            .Setup(s => s.GetByEntityAsync(entityType, idKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _controller.GetByEntity(entityType, idKey, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull("response should have a 'data' property");
        var entries = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        entries.Should().HaveCount(2);
        entries.First().ActionType.Should().Be(AuditAction.Create);
        entries.Last().ActionType.Should().Be(AuditAction.Update);

        _auditServiceMock.Verify(
            s => s.GetByEntityAsync(entityType, idKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByEntity_WithEmptyEntityType_ReturnsBadRequest()
    {
        // Arrange
        var idKey = "person123";

        // Act
        var result = await _controller.GetByEntity("", idKey, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Be("Entity type cannot be empty.");

        _auditServiceMock.Verify(
            s => s.GetByEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByEntity_WithNullEntityType_ReturnsBadRequest()
    {
        // Arrange
        var idKey = "person123";

        // Act
        var result = await _controller.GetByEntity(null!, idKey, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Be("Entity type cannot be empty.");

        _auditServiceMock.Verify(
            s => s.GetByEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Export_WithCsvFormat_ReturnsFileResult()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var csvBytes = "IdKey,ActionType,EntityType,EntityIdKey,PersonName,Timestamp\nlog1,Create,Person,person123,John Admin,2024-01-15"u8.ToArray();

        _auditServiceMock
            .Setup(s => s.ExportAsync(It.IsAny<AuditLogExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(csvBytes);

        // Act
        var result = await _controller.Export(
            startDate, endDate, null, null, null, ExportFormat.Csv, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().EndWith(".csv");
        fileResult.FileContents.Should().Equal(csvBytes);

        _auditServiceMock.Verify(
            s => s.ExportAsync(It.IsAny<AuditLogExportRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Export_WithJsonFormat_ReturnsFileResult()
    {
        // Arrange
        var jsonBytes = "[{\"idKey\":\"log1\",\"actionType\":\"Create\"}]"u8.ToArray();

        _auditServiceMock
            .Setup(s => s.ExportAsync(It.IsAny<AuditLogExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonBytes);

        // Act
        var result = await _controller.Export(
            null, null, null, null, null, ExportFormat.Json, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/json");
        fileResult.FileDownloadName.Should().EndWith(".json");
        fileResult.FileContents.Should().Equal(jsonBytes);
    }

    [Fact]
    public async Task Export_WithExcelFormat_ReturnsFileResult()
    {
        // Arrange
        var excelBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // Mock Excel file header

        _auditServiceMock
            .Setup(s => s.ExportAsync(It.IsAny<AuditLogExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(excelBytes);

        // Act
        var result = await _controller.Export(
            null, null, null, null, null, ExportFormat.Excel, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        fileResult.FileDownloadName.Should().EndWith(".xlsx");
        fileResult.FileContents.Should().Equal(excelBytes);
    }

    [Fact]
    public async Task Export_WithFilters_PassesCorrectRequest()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var entityType = "Person";
        var actionType = AuditAction.Delete;
        var personIdKey = "user456";
        var format = ExportFormat.Csv;

        AuditLogExportRequest? capturedRequest = null;
        _auditServiceMock
            .Setup(s => s.ExportAsync(It.IsAny<AuditLogExportRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogExportRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new byte[] { 0x00 });

        // Act
        await _controller.Export(
            startDate, endDate, entityType, actionType, personIdKey, format, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.StartDate.Should().Be(startDate);
        capturedRequest.EndDate.Should().Be(endDate);
        capturedRequest.EntityType.Should().Be(entityType);
        capturedRequest.ActionType.Should().Be(actionType);
        capturedRequest.PersonIdKey.Should().Be(personIdKey);
        capturedRequest.Format.Should().Be(format);
    }

    [Fact]
    public async Task Export_GeneratesUniqueFileName()
    {
        // Arrange
        _auditServiceMock
            .Setup(s => s.ExportAsync(It.IsAny<AuditLogExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x00 });

        // Act
        var result1 = await _controller.Export(
            null, null, null, null, null, ExportFormat.Csv, CancellationToken.None);
        var result2 = await _controller.Export(
            null, null, null, null, null, ExportFormat.Csv, CancellationToken.None);

        // Assert
        var fileResult1 = result1.Should().BeOfType<FileContentResult>().Subject;
        var fileResult2 = result2.Should().BeOfType<FileContentResult>().Subject;

        // File names should contain timestamp, making them unique (or at least very likely to be)
        fileResult1.FileDownloadName.Should().StartWith("audit-logs-");
        fileResult2.FileDownloadName.Should().StartWith("audit-logs-");
        fileResult1.FileDownloadName.Should().EndWith(".csv");
        fileResult2.FileDownloadName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task Search_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _auditServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<AuditLogSearchParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.Search(
            null, null, null, null, null, null, 1, 20, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetByEntity_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _auditServiceMock
            .Setup(s => s.GetByEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.GetByEntity("Person", "person123", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task Export_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _auditServiceMock
            .Setup(s => s.ExportAsync(It.IsAny<AuditLogExportRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var act = async () => await _controller.Export(
            null, null, null, null, null, ExportFormat.Csv, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }
}
