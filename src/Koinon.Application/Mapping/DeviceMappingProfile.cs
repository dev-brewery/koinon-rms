using AutoMapper;
using Koinon.Application.DTOs;
using Koinon.Domain.Entities;

namespace Koinon.Application.Mapping;

public class DeviceMappingProfile : Profile
{
    public DeviceMappingProfile()
    {
        CreateMap<Device, DeviceDetailDto>()
            .ForMember(d => d.IdKey, opt => opt.MapFrom(s => s.IdKey))
            .ForMember(d => d.DeviceTypeName, opt => opt.MapFrom(s => s.DeviceTypeValue != null ? s.DeviceTypeValue.Value : null))
            .ForMember(d => d.CampusIdKey, opt => opt.MapFrom(s => s.Campus != null ? s.Campus.IdKey : null))
            .ForMember(d => d.CampusName, opt => opt.MapFrom(s => s.Campus != null ? s.Campus.Name : null))
            .ForMember(d => d.HasKioskToken, opt => opt.MapFrom(s => s.KioskToken != null));

        CreateMap<Device, DeviceSummaryDto>()
            .ForMember(d => d.IdKey, opt => opt.MapFrom(s => s.IdKey))
            .ForMember(d => d.DeviceTypeName, opt => opt.MapFrom(s => s.DeviceTypeValue != null ? s.DeviceTypeValue.Value : null))
            .ForMember(d => d.CampusName, opt => opt.MapFrom(s => s.Campus != null ? s.Campus.Name : null));
    }
}
