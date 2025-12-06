using AutoMapper;
using Koinon.Application.Constants;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Person entity mappings.
/// </summary>
public class PersonMappingProfile : Profile
{
    public PersonMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Person, PersonDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.Age, o => o.MapFrom(s => CalculateAge(s.BirthDate)))
            .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender.ToString()))
            .ForMember(d => d.EmailPreference, o => o.MapFrom(s => s.EmailPreference.ToString()))
            .ForMember(d => d.PhoneNumbers, o => o.MapFrom(s => s.PhoneNumbers))
            .ForMember(d => d.RecordStatus, o => o.MapFrom(s => s.RecordStatusValue))
            .ForMember(d => d.ConnectionStatus, o => o.MapFrom(s => s.ConnectionStatusValue))
            .ForMember(d => d.PrimaryFamily, o => o.MapFrom(s =>
                s.PrimaryFamily != null ? new FamilySummaryDto
                {
                    IdKey = s.PrimaryFamily.IdKey,
                    Name = s.PrimaryFamily.Name,
                    MemberCount = 0 // Will be calculated separately if needed
                } : null))
            .ForMember(d => d.PrimaryCampus, o => o.MapFrom(s => s.PrimaryCampus))
            .ForMember(d => d.PhotoUrl, o => o.Ignore()); // Future: map from PhotoId

        CreateMap<Person, PersonSummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.Age, o => o.MapFrom(s => CalculateAge(s.BirthDate)))
            .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender.ToString()))
            .ForMember(d => d.PhotoUrl, o => o.Ignore())
            .ForMember(d => d.ConnectionStatus, o => o.Ignore())
            .ForMember(d => d.RecordStatus, o => o.Ignore());

        CreateMap<PhoneNumber, PhoneNumberDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.NumberFormatted, o => o.MapFrom(s => FormatPhoneNumber(s.Number)))
            .ForMember(d => d.PhoneType, o => o.Ignore()); // Will be set from navigation

        CreateMap<DefinedValue, DefinedValueDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        CreateMap<Group, FamilySummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.MemberCount, o => o.Ignore()); // Calculated separately

        CreateMap<Campus, CampusSummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        // Request to Entity mappings
        CreateMap<CreatePersonRequest, Person>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CreatedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.ModifiedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.Gender, o => o.MapFrom(s => ParseGender(s.Gender)))
            .ForMember(d => d.BirthDay, o => o.MapFrom(s => s.BirthDate.HasValue ? s.BirthDate.Value.Day : (int?)null))
            .ForMember(d => d.BirthMonth, o => o.MapFrom(s => s.BirthDate.HasValue ? s.BirthDate.Value.Month : (int?)null))
            .ForMember(d => d.BirthYear, o => o.MapFrom(s => s.BirthDate.HasValue ? s.BirthDate.Value.Year : (int?)null))
            .ForMember(d => d.PhoneNumbers, o => o.Ignore())
            .ForMember(d => d.GroupMemberships, o => o.Ignore())
            .ForMember(d => d.PersonAliases, o => o.Ignore())
            .ForMember(d => d.PrimaryFamily, o => o.Ignore())
            .ForMember(d => d.IsSystem, o => o.MapFrom(s => false))
            .ForMember(d => d.IsDeceased, o => o.MapFrom(s => false))
            .ForMember(d => d.IsEmailActive, o => o.MapFrom(s => true));

        CreateMap<CreatePhoneNumberRequest, PhoneNumber>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CreatedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.ModifiedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.PersonId, o => o.Ignore())
            .ForMember(d => d.Person, o => o.Ignore())
            .ForMember(d => d.NumberTypeValue, o => o.Ignore())
            .ForMember(d => d.Description, o => o.Ignore())
            .ForMember(d => d.CountryCode, o => o.MapFrom(s => "1"))
            .ForMember(d => d.IsMessagingEnabled, o => o.MapFrom(s => s.IsMessagingEnabled ?? true))
            .ForMember(d => d.IsUnlisted, o => o.MapFrom(s => false));
    }

    private static int? CalculateAge(DateOnly? birthDate)
    {
        if (!birthDate.HasValue)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private static Gender ParseGender(string? gender)
    {
        return gender switch
        {
            GenderValues.Male => Gender.Male,
            GenderValues.Female => Gender.Female,
            _ => Gender.Unknown
        };
    }

    private static string FormatPhoneNumber(string number)
    {
        // Simple US phone number formatting
        // Remove all non-digit characters
        var digits = new string(number.Where(char.IsDigit).ToArray());

        if (digits.Length == 10)
        {
            return $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
        }
        else if (digits.Length == 11 && digits[0] == '1')
        {
            return $"+1 ({digits[1..4]}) {digits[4..7]}-{digits[7..]}";
        }

        // Return as-is if not a standard format
        return number;
    }
}
