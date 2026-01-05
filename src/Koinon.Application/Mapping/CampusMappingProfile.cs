using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for Campus entity mappings.
/// </summary>
public class CampusMappingProfile : Profile
{
    public CampusMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Campus, CampusDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        CreateMap<Campus, CampusSummaryDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        // Request to Entity mappings
        CreateMap<CreateCampusRequest, Campus>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CreatedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.ModifiedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(s => true));

        CreateMap<UpdateCampusRequest, Campus>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Guid, o => o.Ignore())
            .ForMember(d => d.CreatedDateTime, o => o.Ignore())
            .ForMember(d => d.ModifiedDateTime, o => o.Ignore())
            .ForMember(d => d.CreatedByPersonAliasId, o => o.Ignore())
            .ForMember(d => d.ModifiedByPersonAliasId, o => o.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
