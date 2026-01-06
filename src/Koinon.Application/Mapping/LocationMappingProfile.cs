using System.Collections.Generic;
using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

public class LocationMappingProfile : Profile
{
    public LocationMappingProfile()
    {
        CreateMap<Location, LocationDto>()
            .ForMember(d => d.IdKey, opt => opt.MapFrom(s => s.IdKey))
            .ForMember(d => d.ParentLocationName, opt => opt.MapFrom(s => s.ParentLocation != null ? s.ParentLocation.Name : null))
            .ForMember(d => d.CampusName, opt => opt.MapFrom(s => s.Campus != null ? s.Campus.Name : null))
            .ForMember(d => d.LocationTypeName, opt => opt.MapFrom(s => s.LocationTypeValue != null ? s.LocationTypeValue.Value : null))
            .ForMember(d => d.OverflowLocationName, opt => opt.MapFrom(s => s.OverflowLocation != null ? s.OverflowLocation.Name : null))
            // IdKey mapping for relationships
            .ForMember(d => d.ParentLocationIdKey, opt => opt.MapFrom(s => s.ParentLocation != null ? s.ParentLocation.IdKey : null))
            .ForMember(d => d.CampusIdKey, opt => opt.MapFrom(s => s.Campus != null ? s.Campus.IdKey : null))
            .ForMember(d => d.OverflowLocationIdKey, opt => opt.MapFrom(s => s.OverflowLocation != null ? s.OverflowLocation.IdKey : null))
            // Children mapping - ignored here as it's typically handled manually or requires recursion
            .ForMember(d => d.Children, opt => opt.MapFrom(s => new List<LocationDto>()));

        CreateMap<Location, LocationSummaryDto>()
            .ForMember(d => d.IdKey, opt => opt.MapFrom(s => s.IdKey))
            .ForMember(d => d.ParentLocationName, opt => opt.MapFrom(s => s.ParentLocation != null ? s.ParentLocation.Name : null))
            .ForMember(d => d.CampusName, opt => opt.MapFrom(s => s.Campus != null ? s.Campus.Name : null))
            .ForMember(d => d.LocationTypeName, opt => opt.MapFrom(s => s.LocationTypeValue != null ? s.LocationTypeValue.Value : null));
    }
}
