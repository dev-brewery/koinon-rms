using AutoMapper;
using FluentAssertions;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Unit tests for CommunicationService.
/// </summary>
public class CommunicationServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<ILogger<CommunicationService>> _loggerMock;

    public CommunicationServiceTests()
    {
        // Setup in-memory database with full KoinonDbContext
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        _mapperMock = new Mock<IMapper>();
        _userContextMock = new Mock<IUserContext>();
        _loggerMock = new Mock<ILogger<CommunicationService>>();
    }

    [Fact]
    public async Task GetByIdKeyAsync_WithValidIdKey_ReturnsCommunication()
    {
        // Arrange
        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Test Subject",
            Body = "Test Body",
            FromEmail = "test@example.com",
            RecipientCount = 1,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var expectedDto = new CommunicationDto
        {
            IdKey = communication.IdKey,
            Guid = communication.Guid,
            CommunicationType = "Email",
            Status = "Draft",
            Subject = "Test Subject",
            Body = "Test Body",
            RecipientCount = 1,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedDateTime = communication.CreatedDateTime,
            Recipients = new List<CommunicationRecipientDto>()
        };

        _mapperMock.Setup(x => x.Map<CommunicationDto>(It.IsAny<Communication>()))
            .Returns(expectedDto);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetByIdKeyAsync(communication.IdKey);

        // Assert
        result.Should().NotBeNull();
        result!.IdKey.Should().Be(communication.IdKey);
        result.Subject.Should().Be("Test Subject");
    }

    [Fact]
    public async Task GetByIdKeyAsync_WithInvalidIdKey_ReturnsNull()
    {
        // Arrange
        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetByIdKeyAsync("invalid-idkey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdKeyAsync_WithUnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Test Subject",
            Body = "Test Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        // Different person trying to access
        _userContextMock.Setup(x => x.CurrentPersonId).Returns(999);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetByIdKeyAsync(communication.IdKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdKeyAsync_WithStaffRole_ReturnsAnyonesCommunication()
    {
        // Arrange
        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Test Subject",
            Body = "Test Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(999);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(true);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var expectedDto = new CommunicationDto
        {
            IdKey = communication.IdKey,
            Guid = communication.Guid,
            CommunicationType = "Email",
            Status = "Draft",
            Subject = "Test Subject",
            Body = "Test Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedDateTime = communication.CreatedDateTime,
            Recipients = new List<CommunicationRecipientDto>()
        };

        _mapperMock.Setup(x => x.Map<CommunicationDto>(It.IsAny<Communication>()))
            .Returns(expectedDto);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetByIdKeyAsync(communication.IdKey);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesSuccessfully()
    {
        // Arrange

        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Gender = Gender.Male
        };
        _context.People.Add(person);

        var personAlias = new PersonAlias { PersonId = person.Id };
        _context.PersonAliases.Add(personAlias);

        var groupType = new GroupType { Name = "Small Group" };
        _context.GroupTypes.Add(groupType);

        var groupRole = new GroupTypeRole
        {
            GroupTypeId = groupType.Id,
            Name = "Leader",
            IsLeader = true
        };
        _context.GroupTypeRoles.Add(groupRole);

        var group = new Group
        {
            Name = "Test Group",
            GroupTypeId = groupType.Id,
            IsActive = true
        };
        _context.Groups.Add(group);

        var groupMember = new GroupMember
        {
            GroupId = group.Id,
            PersonId = person.Id,
            GroupRoleId = groupRole.Id,
            GroupMemberStatus = GroupMemberStatus.Active
        };
        _context.GroupMembers.Add(groupMember);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(person.Id);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test Subject",
            Body = "Test Body",
            FromEmail = "from@example.com",
            FromName = "From Name",
            GroupIdKeys = new List<string> { group.IdKey }
        };

        _mapperMock.Setup(x => x.Map<CommunicationDto>(It.IsAny<Communication>()))
            .Returns((Communication c) => new CommunicationDto
            {
                IdKey = c.IdKey,
                Guid = c.Guid,
                CommunicationType = "Email",
                Status = "Draft",
                Subject = "Test Subject",
                Body = "Test Body",
                RecipientCount = 1,
                DeliveredCount = 0,
                FailedCount = 0,
                OpenedCount = 0,
                CreatedDateTime = c.CreatedDateTime,
                Recipients = new List<CommunicationRecipientDto>()
            });

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RecipientCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCommunicationType_ReturnsFailure()
    {
        // Arrange
        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        var dto = new CreateCommunicationDto
        {
            CommunicationType = "InvalidType",
            Subject = "Test",
            Body = "Test Body",
            GroupIdKeys = new List<string> { "ABC123" }
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INVALID_TYPE");
    }

    [Fact]
    public async Task CreateAsync_WithMoreThan50Groups_ReturnsFailure()
    {
        // Arrange
        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        var groupIdKeys = Enumerable.Range(1, 51).Select(i => $"GROUP{i}").ToList();
        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            GroupIdKeys = groupIdKeys
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("more than 50 groups");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidGroupIdKey_ReturnsFailure()
    {
        // Arrange
        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            GroupIdKeys = new List<string> { "invalid-idkey" }
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_WithNoRecipients_ReturnsFailure()
    {
        // Arrange

        var groupType = new GroupType { Name = "Small Group" };
        _context.GroupTypes.Add(groupType);

        var groupRole = new GroupTypeRole
        {
            GroupTypeId = groupType.Id,
            Name = "Leader",
            IsLeader = true
        };
        _context.GroupTypeRoles.Add(groupRole);

        var group = new Group
        {
            Name = "Empty Group",
            GroupTypeId = groupType.Id,
            IsActive = true
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(true);

        var dto = new CreateCommunicationDto
        {
            CommunicationType = "Email",
            Subject = "Test",
            Body = "Test Body",
            FromEmail = "from@example.com",
            GroupIdKeys = new List<string> { group.IdKey }
        };

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NO_RECIPIENTS");
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesSuccessfully()
    {
        // Arrange

        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Original Subject",
            Body = "Original Body",
            FromEmail = "original@example.com",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var updateDto = new UpdateCommunicationDto
        {
            Subject = "Updated Subject",
            Body = "Updated Body"
        };

        var expectedDto = new CommunicationDto
        {
            IdKey = communication.IdKey,
            Guid = communication.Guid,
            CommunicationType = "Email",
            Status = "Draft",
            Subject = "Updated Subject",
            Body = "Updated Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedDateTime = communication.CreatedDateTime,
            Recipients = new List<CommunicationRecipientDto>()
        };

        _mapperMock.Setup(x => x.Map<CommunicationDto>(It.IsAny<Communication>()))
            .Returns(expectedDto);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.UpdateAsync(communication.IdKey, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Subject.Should().Be("Updated Subject");
    }

    [Fact]
    public async Task UpdateAsync_WithNonDraftStatus_ReturnsFailure()
    {
        // Arrange

        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Subject = "Sent Subject",
            Body = "Sent Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var updateDto = new UpdateCommunicationDto { Subject = "New Subject" };

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.UpdateAsync(communication.IdKey, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("Only draft communications can be updated");
    }

    [Fact]
    public async Task DeleteAsync_WithDraftCommunication_DeletesSuccessfully()
    {
        // Arrange

        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.DeleteAsync(communication.IdKey);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var deletedComm = await _context.Communications.FindAsync(communication.Id);
        deletedComm.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonDraftStatus_ReturnsFailure()
    {
        // Arrange

        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.DeleteAsync(communication.IdKey);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("Only draft communications can be deleted");
    }

    [Fact]
    public async Task SendAsync_WithDraftCommunication_QueuesCommunication()
    {
        // Arrange

        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 1,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var expectedDto = new CommunicationDto
        {
            IdKey = communication.IdKey,
            Guid = communication.Guid,
            CommunicationType = "Email",
            Status = "Pending",
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 1,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedDateTime = communication.CreatedDateTime,
            Recipients = new List<CommunicationRecipientDto>()
        };

        _mapperMock.Setup(x => x.Map<CommunicationDto>(It.IsAny<Communication>()))
            .Returns(expectedDto);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SendAsync(communication.IdKey);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Pending");

        var updatedComm = await _context.Communications.FindAsync(communication.Id);
        updatedComm!.Status.Should().Be(CommunicationStatus.Pending);
    }

    [Fact]
    public async Task SendAsync_WithNoRecipients_ReturnsFailure()
    {
        // Arrange

        var personAlias = new PersonAlias { PersonId = 1 };
        _context.PersonAliases.Add(personAlias);

        var communication = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Test",
            Body = "Test Body",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias.Id
        };
        _context.Communications.Add(communication);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SendAsync(communication.IdKey);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("Cannot send communication with no recipients");
    }

    [Fact]
    public async Task SearchAsync_WithStaffRole_ReturnsAllCommunications()
    {
        // Arrange

        var comm1 = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "Test 1",
            Body = "Body 1",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0
        };
        var comm2 = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Subject = "Test 2",
            Body = "Body 2",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0
        };
        _context.Communications.AddRange(comm1, comm2);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(true);

        _mapperMock.Setup(x => x.Map<CommunicationSummaryDto>(It.IsAny<Communication>()))
            .Returns((Communication c) => new CommunicationSummaryDto
            {
                IdKey = c.IdKey,
                CommunicationType = c.CommunicationType.ToString(),
                Status = c.Status.ToString(),
                Subject = c.Subject,
                RecipientCount = c.RecipientCount,
                DeliveredCount = c.DeliveredCount,
                FailedCount = c.FailedCount,
                CreatedDateTime = c.CreatedDateTime,
                SentDateTime = c.SentDateTime
            });

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SearchAsync(page: 1, pageSize: 20);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_WithNonStaffRole_ReturnsOnlyOwnCommunications()
    {
        // Arrange

        var personAlias1 = new PersonAlias { PersonId = 1 };
        var personAlias2 = new PersonAlias { PersonId = 2 };
        _context.PersonAliases.AddRange(personAlias1, personAlias2);

        var comm1 = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Subject = "My Communication",
            Body = "Body 1",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias1.Id
        };
        var comm2 = new Communication
        {
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Subject = "Other's Communication",
            Body = "Body 2",
            RecipientCount = 0,
            DeliveredCount = 0,
            FailedCount = 0,
            OpenedCount = 0,
            CreatedByPersonAliasId = personAlias2.Id
        };
        _context.Communications.AddRange(comm1, comm2);
        await _context.SaveChangesAsync();

        _userContextMock.Setup(x => x.CurrentPersonId).Returns(1);
        _userContextMock.Setup(x => x.IsInRole("Staff")).Returns(false);
        _userContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        _mapperMock.Setup(x => x.Map<CommunicationSummaryDto>(It.IsAny<Communication>()))
            .Returns((Communication c) => new CommunicationSummaryDto
            {
                IdKey = c.IdKey,
                CommunicationType = c.CommunicationType.ToString(),
                Status = c.Status.ToString(),
                Subject = c.Subject,
                RecipientCount = c.RecipientCount,
                DeliveredCount = c.DeliveredCount,
                FailedCount = c.FailedCount,
                CreatedDateTime = c.CreatedDateTime,
                SentDateTime = c.SentDateTime
            });

        var service = new CommunicationService(_context, _mapperMock.Object, _userContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SearchAsync(page: 1, pageSize: 20);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Subject.Should().Be("My Communication");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
