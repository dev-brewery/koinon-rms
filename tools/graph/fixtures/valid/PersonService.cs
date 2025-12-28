using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;

namespace Koinon.Application.Services;

/// <summary>
/// Person service for testing graph generation.
/// Follows project conventions: async methods with CancellationToken.
/// </summary>
public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;

    public PersonService(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    /// <summary>
    /// Gets a person by IdKey asynchronously.
    /// </summary>
    public async Task<PersonDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        var person = await _personRepository.GetByIdKeyAsync(idKey, ct);

        if (person == null)
        {
            return null;
        }

        return MapToDto(person);
    }

    /// <summary>
    /// Searches for people asynchronously.
    /// </summary>
    public async Task<PagedResult<PersonSummaryDto>> SearchAsync(
        PersonSearchParameters parameters,
        CancellationToken ct = default)
    {
        var result = await _personRepository.SearchAsync(parameters, ct);

        return new PagedResult<PersonSummaryDto>
        {
            Items = result.Items.Select(MapToSummaryDto).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Creates a new person asynchronously.
    /// </summary>
    public async Task<Result<PersonDto>> CreateAsync(
        CreatePersonRequest request,
        CancellationToken ct = default)
    {
        // Validation would go here

        var person = new Person
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Gender = request.Gender ?? Gender.Unknown
        };

        await _personRepository.AddAsync(person, ct);
        await _personRepository.SaveChangesAsync(ct);

        return Result<PersonDto>.Success(MapToDto(person));
    }

    private PersonDto MapToDto(Person person)
    {
        return new PersonDto
        {
            IdKey = person.IdKey,
            Guid = person.Guid,
            FirstName = person.FirstName,
            NickName = person.NickName,
            LastName = person.LastName,
            FullName = person.FullName,
            Email = person.Email,
            IsEmailActive = true,
            BirthDate = person.BirthDate,
            Age = CalculateAge(person.BirthDate),
            Gender = person.Gender.ToString(),
            PhoneNumbers = Array.Empty<PhoneNumberDto>(),
            CreatedDateTime = person.CreatedDateTime,
            ModifiedDateTime = person.ModifiedDateTime
        };
    }

    private PersonSummaryDto MapToSummaryDto(Person person)
    {
        return new PersonSummaryDto
        {
            IdKey = person.IdKey,
            FirstName = person.FirstName,
            NickName = person.NickName,
            LastName = person.LastName,
            FullName = person.FullName,
            Email = person.Email,
            Age = CalculateAge(person.BirthDate)
        };
    }

    private int? CalculateAge(DateOnly? birthDate)
    {
        if (!birthDate.HasValue)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;

        if (birthDate.Value > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
