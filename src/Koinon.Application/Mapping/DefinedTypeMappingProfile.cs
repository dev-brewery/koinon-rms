using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

/// <summary>
/// AutoMapper profile for DefinedType and DefinedValue entity mappings.
/// </summary>
public class DefinedTypeMappingProfile : Profile
{
    public DefinedTypeMappingProfile()
    {
        CreateMap<DefinedType, DefinedTypeDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.DefinedValues, o => o.MapFrom(s => s.DefinedValues));

        // Embedded DTO used by DefinedTypeDto.DefinedValues and by PersonDto fields
        CreateMap<DefinedValue, DefinedValueDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey));

        // Full management DTO returned by CRUD endpoints
        CreateMap<DefinedValue, DefinedValueManagementDto>()
            .ForMember(d => d.IdKey, o => o.MapFrom(s => s.IdKey))
            .ForMember(d => d.DefinedTypeIdKey, o => o.MapFrom(s => s.DefinedType != null ? s.DefinedType.IdKey : string.Empty));
    }
}
