using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Communication-related entity mappings.
/// </summary>
public class CommunicationMappingProfile : Profile
{
    public CommunicationMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Communication, CommunicationDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.CommunicationType, o => o.MapFrom(s => s.CommunicationType.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Recipients, o => o.MapFrom(s => s.Recipients));

        CreateMap<Communication, CommunicationSummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.CommunicationType, o => o.MapFrom(s => s.CommunicationType.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<CommunicationRecipient, CommunicationRecipientDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.PersonIdKey, o => o.MapFrom(s => IdKeyHelper.Encode(s.PersonId)))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.GroupIdKey, o => o.MapFrom(s => s.GroupId.HasValue ? IdKeyHelper.Encode(s.GroupId.Value) : null));
    }
}
