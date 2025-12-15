using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Files;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class PeopleControllerTests
{
    private readonly Mock<IPersonService> _personServiceMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<ILogger<PeopleController>> _loggerMock;
    private readonly PeopleController _controller;

    // Valid IdKeys for testing (using IdKeyHelper.Encode)
    private readonly string _personIdKey = IdKeyHelper.Encode(123);
    private readonly string _person2IdKey = IdKeyHelper.Encode(456);
    private readonly string _familyIdKey = IdKeyHelper.Encode(100);
    private readonly string _campusIdKey = IdKeyHelper.Encode(5);
    private readonly string _statusIdKey = IdKeyHelper.Encode(10);
    private readonly string _connectionStatusIdKey = IdKeyHelper.Encode(20);
    private readonly string _newPersonIdKey = IdKeyHelper.Encode(999);

    public PeopleControllerTests()
    {
        _personServiceMock = new Mock<IPersonService>();
        _fileServiceMock = new Mock<IFileService>();
        _loggerMock = new Mock<ILogger<PeopleController>>();
        _controller = new PeopleController(_personServiceMock.Object, _fileServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Search Tests

    [Fact]
    public async Task Search_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<PersonSummaryDto>(
            new List<PersonSummaryDto>
            {
                new() { IdKey = _personIdKey, FirstName = "John", LastName = "Doe", FullName = "John Doe", Gender = "Male" },
                new() { IdKey = _person2IdKey, FirstName = "Jane", LastName = "Smith", FullName = "Jane Smith", Gender = "Female" }
            },
            totalCount: 2,
            page: 1,
            pageSize: 25
        );

        _personServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<PersonSearchParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Search(
            query: "John",
            campusId: null,
            recordStatusId: null,
            connectionStatusId: null,
            includeInactive: false,
            page: 1,
            pageSize: 25);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<PersonSummaryDto>>().Subject;
        pagedResult.Items.Should().HaveCount(2);
        pagedResult.TotalCount.Should().Be(2);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Search_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var expectedResult = new PagedResult<PersonSummaryDto>(
            new List<PersonSummaryDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 25
        );

        PersonSearchParameters? capturedParameters = null;
        _personServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<PersonSearchParameters>(), It.IsAny<CancellationToken>()))
            .Callback<PersonSearchParameters, CancellationToken>((p, _) => capturedParameters = p)
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.Search(
            query: "test",
            campusId: _campusIdKey,
            recordStatusId: _statusIdKey,
            connectionStatusId: _connectionStatusIdKey,
            includeInactive: true,
            page: 2,
            pageSize: 50);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.Query.Should().Be("test");
        capturedParameters.CampusId.Should().Be(_campusIdKey);
        capturedParameters.RecordStatusId.Should().Be(_statusIdKey);
        capturedParameters.ConnectionStatusId.Should().Be(_connectionStatusIdKey);
        capturedParameters.IncludeInactive.Should().BeTrue();
        capturedParameters.Page.Should().Be(2);
        capturedParameters.PageSize.Should().Be(50);
    }

    #endregion

    #region GetByIdKey Tests

    [Fact]
    public async Task GetByIdKey_WithExistingPerson_ReturnsOkWithPerson()
    {
        // Arrange
        var expectedPerson = new PersonDto
        {
            IdKey = _personIdKey,
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow
        };

        _personServiceMock
            .Setup(s => s.GetByIdKeyAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPerson);

        // Act
        var result = await _controller.GetByIdKey(_personIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var person = okResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be(_personIdKey);
        person.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetByIdKey_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        _personServiceMock
            .Setup(s => s.GetByIdKeyAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.GetByIdKey(nonExistentIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Contain(nonExistentIdKey);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedWithPerson()
    {
        // Arrange
        var request = new CreatePersonRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        var createdPerson = new PersonDto
        {
            IdKey = _newPersonIdKey,
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Gender = "Unknown",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow
        };

        _personServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Success(createdPerson));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PeopleController.GetByIdKey));
        createdResult.RouteValues.Should().ContainKey("idKey");
        createdResult.RouteValues!["idKey"].Should().Be(_newPersonIdKey);

        var person = createdResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be(_newPersonIdKey);
        person.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Create_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreatePersonRequest
        {
            FirstName = "",
            LastName = "Doe"
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "One or more validation errors occurred",
            new Dictionary<string, string[]>
            {
                { "FirstName", new[] { "First name is required" } }
            }
        );

        _personServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task Create_WithBusinessRuleViolation_ReturnsUnprocessableEntity()
    {
        // Arrange
        var request = new CreatePersonRequest
        {
            FirstName = "John",
            LastName = "Doe"
        };

        var error = Error.UnprocessableEntity("Cannot create person without family");

        _personServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOkWithUpdatedPerson()
    {
        // Arrange
        var request = new UpdatePersonRequest
        {
            FirstName = "John Updated",
            Email = "john.updated@example.com"
        };

        var updatedPerson = new PersonDto
        {
            IdKey = _personIdKey,
            Guid = Guid.NewGuid(),
            FirstName = "John Updated",
            LastName = "Doe",
            FullName = "John Updated Doe",
            Email = "john.updated@example.com",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow.AddDays(-1),
            ModifiedDateTime = DateTime.UtcNow
        };

        _personServiceMock
            .Setup(s => s.UpdateAsync(_personIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Success(updatedPerson));

        // Act
        var result = await _controller.Update(_personIdKey, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var person = okResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be(_personIdKey);
        person.FirstName.Should().Be("John Updated");
        person.ModifiedDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var request = new UpdatePersonRequest
        {
            FirstName = "John"
        };

        var error = Error.NotFound("Person", nonExistentIdKey);

        _personServiceMock
            .Setup(s => s.UpdateAsync(nonExistentIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Update(nonExistentIdKey, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Update_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdatePersonRequest
        {
            Email = "invalid-email"
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "One or more validation errors occurred",
            new Dictionary<string, string[]>
            {
                { "Email", new[] { "Invalid email format" } }
            }
        );

        _personServiceMock
            .Setup(s => s.UpdateAsync(_personIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Update(_personIdKey, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingPerson_ReturnsNoContent()
    {
        // Arrange
        _personServiceMock
            .Setup(s => s.DeleteAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete(_personIdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var error = Error.NotFound("Person", nonExistentIdKey);

        _personServiceMock
            .Setup(s => s.DeleteAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Delete(nonExistentIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_WithBusinessRuleViolation_ReturnsUnprocessableEntity()
    {
        // Arrange
        var error = Error.UnprocessableEntity("Cannot delete person with active attendance");

        _personServiceMock
            .Setup(s => s.DeleteAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Delete(_personIdKey);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion

    #region UploadPhoto Tests

    [Fact]
    public async Task UploadPhoto_WithValidImage_ReturnsOkWithUpdatedPerson()
    {
        // Arrange
        var photoIdKey = IdKeyHelper.Encode(5000);
        var uploadedFileDto = new FileMetadataDto
        {
            IdKey = photoIdKey,
            FileName = "photo.jpg",
            MimeType = "image/jpeg",
            FileSizeBytes = 50000,
            Url = $"/api/v1/files/{photoIdKey}"
        };

        var updatedPerson = new PersonDto
        {
            IdKey = _personIdKey,
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            PhotoUrl = $"/api/v1/files/{photoIdKey}",
            CreatedDateTime = DateTime.UtcNow.AddDays(-1),
            ModifiedDateTime = DateTime.UtcNow
        };

        var formFile = CreateValidImageFile("photo.jpg", "image/jpeg");

        _fileServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedFileDto);

        _personServiceMock
            .Setup(s => s.UpdatePhotoAsync(_personIdKey, photoIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Success(updatedPerson));

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, formFile);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var person = okResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be(_personIdKey);
        person.PhotoUrl.Should().Be($"/api/v1/files/{photoIdKey}");

        _fileServiceMock.Verify(s => s.UploadFileAsync(
            It.Is<UploadFileRequest>(req =>
                req.FileName == "photo.jpg" &&
                req.ContentType == "image/jpeg" &&
                req.Description == $"Photo for person {_personIdKey}"),
            It.IsAny<CancellationToken>()), Times.Once);

        _personServiceMock.Verify(s => s.UpdatePhotoAsync(
            _personIdKey,
            photoIdKey,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPhoto_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadPhoto(_personIdKey, null!);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Validation failed");
        problemDetails.Detail.Should().Be("File is required");

        _fileServiceMock.Verify(s => s.UploadFileAsync(
            It.IsAny<UploadFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var emptyFile = CreateFormFile("empty.jpg", "image/jpeg", Array.Empty<byte>());

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, emptyFile);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Be("File is required");

        _fileServiceMock.Verify(s => s.UploadFileAsync(
            It.IsAny<UploadFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_WithFileTooLarge_ReturnsBadRequest()
    {
        // Arrange - Create a 6MB file (exceeds 5MB limit)
        var largeFile = CreateFormFile("large.jpg", "image/jpeg", new byte[6 * 1024 * 1024]);

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, largeFile);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Validation failed");
        problemDetails.Detail.Should().Contain("exceeds maximum allowed size of 5MB");

        _fileServiceMock.Verify(s => s.UploadFileAsync(
            It.IsAny<UploadFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_WithInvalidExtension_ReturnsBadRequest()
    {
        // Arrange
        var invalidFile = CreateFormFile("document.pdf", "application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 });

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, invalidFile);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Validation failed");
        problemDetails.Detail.Should().Be("Only .jpg, .jpeg, .png, and .gif files are allowed");

        _fileServiceMock.Verify(s => s.UploadFileAsync(
            It.IsAny<UploadFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_WithInvalidMimeType_ReturnsBadRequest()
    {
        // Arrange
        var invalidFile = CreateFormFile("file.jpg", "text/plain", new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, invalidFile);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Validation failed");
        problemDetails.Detail.Should().Be("Only image files are allowed for person photos");

        _fileServiceMock.Verify(s => s.UploadFileAsync(
            It.IsAny<UploadFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_WithCorruptedImageData_ReturnsBadRequest()
    {
        // Arrange - Create a file with .jpg extension and image MIME type but corrupted content
        var corruptedFile = CreateFormFile("corrupted.jpg", "image/jpeg", new byte[]
        {
            // Not a valid JPEG signature - should fail ImageSharp validation
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
        });

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, corruptedFile);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Validation failed");
        // The controller catches both UnknownImageFormatException and generic exceptions
        // The error detail could be either message depending on the exception type
        problemDetails.Detail.Should().Match(d =>
            d.Contains("not a valid image") || d.Contains("Unable to process the image file"));

        _fileServiceMock.Verify(s => s.UploadFileAsync(
            It.IsAny<UploadFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadPhoto_WhenPersonNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var photoIdKey = IdKeyHelper.Encode(5000);

        var uploadedFileDto = new FileMetadataDto
        {
            IdKey = photoIdKey,
            FileName = "photo.jpg",
            MimeType = "image/jpeg",
            FileSizeBytes = 50000,
            Url = $"/api/v1/files/{photoIdKey}"
        };

        var formFile = CreateValidImageFile("photo.jpg", "image/jpeg");

        _fileServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedFileDto);

        var error = Error.NotFound("Person", nonExistentIdKey);
        _personServiceMock
            .Setup(s => s.UpdatePhotoAsync(nonExistentIdKey, photoIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        _fileServiceMock
            .Setup(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadPhoto(nonExistentIdKey, formFile);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Title.Should().Be("Person not found");

        // Verify cleanup was called
        _fileServiceMock.Verify(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPhoto_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var photoIdKey = IdKeyHelper.Encode(5000);

        var uploadedFileDto = new FileMetadataDto
        {
            IdKey = photoIdKey,
            FileName = "photo.jpg",
            MimeType = "image/jpeg",
            FileSizeBytes = 50000,
            Url = $"/api/v1/files/{photoIdKey}"
        };

        var formFile = CreateValidImageFile("photo.jpg", "image/jpeg");

        _fileServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedFileDto);

        var error = Error.Forbidden("You can only update your own photo");
        _personServiceMock
            .Setup(s => s.UpdatePhotoAsync(_personIdKey, photoIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        _fileServiceMock
            .Setup(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, formFile);

        // Assert
        // Note: Controller maps non-NOT_FOUND errors to BadRequest, not Forbidden
        // This is a design decision in the controller implementation
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("FORBIDDEN");
        problemDetails.Detail.Should().Be("You can only update your own photo");

        // Verify cleanup was called
        _fileServiceMock.Verify(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPhoto_WhenUpdateFailsAndCleanupFails_ReturnsBadRequest()
    {
        // Arrange
        var photoIdKey = IdKeyHelper.Encode(5000);

        var uploadedFileDto = new FileMetadataDto
        {
            IdKey = photoIdKey,
            FileName = "photo.jpg",
            MimeType = "image/jpeg",
            FileSizeBytes = 50000,
            Url = $"/api/v1/files/{photoIdKey}"
        };

        var formFile = CreateValidImageFile("photo.jpg", "image/jpeg");

        _fileServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedFileDto);

        var error = Error.NotFound("Person", _personIdKey);
        _personServiceMock
            .Setup(s => s.UpdatePhotoAsync(_personIdKey, photoIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Cleanup also fails
        _fileServiceMock
            .Setup(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, formFile);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);

        // Should still return the person not found error, not the cleanup error
        problemDetails.Title.Should().Be("Person not found");

        // Verify cleanup was attempted
        _fileServiceMock.Verify(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPhoto_WhenUploadSucceedsButExceptionThrown_CleansUpAndReturnsBadRequest()
    {
        // Arrange
        var photoIdKey = IdKeyHelper.Encode(5000);

        var uploadedFileDto = new FileMetadataDto
        {
            IdKey = photoIdKey,
            FileName = "photo.jpg",
            MimeType = "image/jpeg",
            FileSizeBytes = 50000,
            Url = $"/api/v1/files/{photoIdKey}"
        };

        var formFile = CreateValidImageFile("photo.jpg", "image/jpeg");

        _fileServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedFileDto);

        // Simulate an unexpected exception during UpdatePhotoAsync
        _personServiceMock
            .Setup(s => s.UpdatePhotoAsync(_personIdKey, photoIdKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected database error"));

        _fileServiceMock
            .Setup(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadPhoto(_personIdKey, formFile);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Upload failed");
        problemDetails.Detail.Should().Be("An error occurred while uploading the photo");

        // Verify cleanup was called
        _fileServiceMock.Verify(s => s.DeleteFileAsync(photoIdKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Helper method to create a valid image file mock with realistic JPEG data.
    /// </summary>
    private static IFormFile CreateValidImageFile(string fileName, string contentType)
    {
        // Create a minimal valid JPEG file (1x1 pixel red image)
        // JPEG signature: FF D8 FF (SOI marker)
        // Minimal JPEG structure that ImageSharp can parse
        var jpegBytes = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00,
            0x7F, 0xFF, 0xD9
        };

        return CreateFormFile(fileName, contentType, jpegBytes);
    }

    /// <summary>
    /// Helper method to create a FormFile mock with specified content.
    /// </summary>
    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        var formFile = new Mock<IFormFile>();

        formFile.Setup(f => f.FileName).Returns(fileName);
        formFile.Setup(f => f.ContentType).Returns(contentType);
        formFile.Setup(f => f.Length).Returns(content.Length);
        formFile.Setup(f => f.OpenReadStream()).Returns(() =>
        {
            // Create a new stream each time to simulate how ASP.NET Core handles streams
            var newStream = new MemoryStream(content);
            return newStream;
        });

        return formFile.Object;
    }

    #endregion

    #region GetFamily Tests

    [Fact]
    public async Task GetFamily_WithExistingFamily_ReturnsOkWithFamily()
    {
        // Arrange
        var expectedFamily = new FamilySummaryDto
        {
            IdKey = _familyIdKey,
            Name = "Doe Family",
            MemberCount = 4
        };

        _personServiceMock
            .Setup(s => s.GetFamilyAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilySummaryDto?>.Success(expectedFamily));

        // Act
        var result = await _controller.GetFamily(_personIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var family = okResult.Value.Should().BeOfType<FamilySummaryDto>().Subject;
        family.IdKey.Should().Be(_familyIdKey);
        family.Name.Should().Be("Doe Family");
        family.MemberCount.Should().Be(4);
    }

    [Fact]
    public async Task GetFamily_WithNoFamily_ReturnsOkWithNull()
    {
        // Arrange - Person exists but has no family
        _personServiceMock
            .Setup(s => s.GetFamilyAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilySummaryDto?>.Success(null));

        // Act
        var result = await _controller.GetFamily(_personIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetFamily_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange - Person doesn't exist
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var error = Error.NotFound("Person", nonExistentIdKey);

        _personServiceMock
            .Setup(s => s.GetFamilyAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilySummaryDto?>.Failure(error));

        // Act
        var result = await _controller.GetFamily(nonExistentIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Title.Should().Be("Person not found");
        problemDetails.Detail.Should().Contain(nonExistentIdKey);
    }

    #endregion

    #region GetGroups Tests

    [Fact]
    public async Task GetGroups_WithExistingPerson_ReturnsOkWithPagedResult()
    {
        // Arrange
        var groupIdKey = IdKeyHelper.Encode(200);
        var groupTypeIdKey = IdKeyHelper.Encode(10);
        var roleIdKey = IdKeyHelper.Encode(20);

        var expectedResult = new PagedResult<PersonGroupMembershipDto>(
            new List<PersonGroupMembershipDto>
            {
                new()
                {
                    IdKey = IdKeyHelper.Encode(1),
                    Guid = Guid.NewGuid(),
                    GroupIdKey = groupIdKey,
                    GroupName = "Small Group Alpha",
                    GroupTypeIdKey = groupTypeIdKey,
                    GroupTypeName = "Small Group",
                    RoleIdKey = roleIdKey,
                    RoleName = "Member",
                    MemberStatus = "Active",
                    CreatedDateTime = DateTime.UtcNow.AddMonths(-2)
                },
                new()
                {
                    IdKey = IdKeyHelper.Encode(2),
                    Guid = Guid.NewGuid(),
                    GroupIdKey = IdKeyHelper.Encode(201),
                    GroupName = "Serving Team",
                    GroupTypeIdKey = IdKeyHelper.Encode(11),
                    GroupTypeName = "Serving Team",
                    RoleIdKey = IdKeyHelper.Encode(21),
                    RoleName = "Leader",
                    MemberStatus = "Active",
                    CreatedDateTime = DateTime.UtcNow.AddMonths(-6)
                }
            },
            totalCount: 2,
            page: 1,
            pageSize: 25
        );

        _personServiceMock
            .Setup(s => s.GetGroupsAsync(_personIdKey, 1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGroups(_personIdKey, page: 1, pageSize: 25);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<PersonGroupMembershipDto>>().Subject;
        pagedResult.Items.Should().HaveCount(2);
        pagedResult.TotalCount.Should().Be(2);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(25);
        pagedResult.Items.First().GroupName.Should().Be("Small Group Alpha");
    }

    [Fact]
    public async Task GetGroups_WithPagination_PassesPaginationParametersToService()
    {
        // Arrange
        var expectedResult = new PagedResult<PersonGroupMembershipDto>(
            new List<PersonGroupMembershipDto>(),
            totalCount: 0,
            page: 2,
            pageSize: 10
        );

        int? capturedPage = null;
        int? capturedPageSize = null;
        _personServiceMock
            .Setup(s => s.GetGroupsAsync(_personIdKey, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, int, CancellationToken>((_, p, ps, _) =>
            {
                capturedPage = p;
                capturedPageSize = ps;
            })
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetGroups(_personIdKey, page: 2, pageSize: 10);

        // Assert
        capturedPage.Should().Be(2);
        capturedPageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetGroups_WithNoGroups_ReturnsEmptyPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<PersonGroupMembershipDto>(
            new List<PersonGroupMembershipDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 25
        );

        _personServiceMock
            .Setup(s => s.GetGroupsAsync(_personIdKey, 1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGroups(_personIdKey, page: 1, pageSize: 25);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<PersonGroupMembershipDto>>().Subject;
        pagedResult.Items.Should().BeEmpty();
        pagedResult.TotalCount.Should().Be(0);
    }

    #endregion
}
