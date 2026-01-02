using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for CommunicationTemplate-related entity mappings.
/// </summary>
public class CommunicationTemplateMappingProfile : Profile
{
    public CommunicationTemplateMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<CommunicationTemplate, CommunicationTemplateDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.CommunicationType, o => o.MapFrom(s => s.CommunicationType.ToString()));

        CreateMap<CommunicationTemplate, CommunicationTemplateSummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.CommunicationType, o => o.MapFrom(s => s.CommunicationType.ToString()));
    }
}
