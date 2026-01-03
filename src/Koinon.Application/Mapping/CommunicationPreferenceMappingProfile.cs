using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for CommunicationPreference entity mappings.
/// </summary>
public class CommunicationPreferenceMappingProfile : Profile
{
    public CommunicationPreferenceMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<CommunicationPreference, CommunicationPreferenceDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.PersonIdKey, o => o.MapFrom(s => IdKeyHelper.Encode(s.PersonId)))
            .ForMember(d => d.CommunicationType, o => o.MapFrom(s => s.CommunicationType.ToString()));
    }
}
