using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Label entity mappings.
/// </summary>
public class LabelMappingProfile : Profile
{
    public LabelMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<LabelTemplate, LabelTemplateDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));
    }
}
